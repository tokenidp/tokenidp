using Microsoft.Extensions.Options;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Core.Admin.Roles;
using TokenIDP.Core.Admin.Tenants;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Domain;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Infrastructure.Bootstrap.SeedData;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

internal sealed class TenantBootstrapper : ITenantBootstrapper
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRoleProvisioningService _roleProvisioningService;
    private readonly IClientProvisioningService _clientProvisioningService;
    private readonly IUserProvisioningService _userProvisioningService;
    private readonly IPermissionSeeder _permissionSeeder;
    private readonly IConfigurationSeeder _configurationSeeder;
    private readonly ICodeSequenceGenerator _codeSequenceGenerator;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly BootstrapOption _bootstrapOptions;
    private readonly IAppLogger<TenantBootstrapper> _logger;

    public TenantBootstrapper(
        ApplicationDbContext dbContext,
        IRoleProvisioningService roleProvisioningService,
        IClientProvisioningService clientProvisioningService,
        IUserProvisioningService userProvisioningService,
        IPermissionSeeder permissionSeeder,
        IConfigurationSeeder configurationSeeder,
        ICodeSequenceGenerator codeSequenceGenerator,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserService currentUserService,
        IOptions<BootstrapOption> bootstrapOptions,
        IAppLogger<TenantBootstrapper> logger)
    {
        _dbContext = dbContext;
        _roleProvisioningService = roleProvisioningService;
        _clientProvisioningService = clientProvisioningService;
        _userProvisioningService = userProvisioningService;
        _permissionSeeder = permissionSeeder;
        _configurationSeeder = configurationSeeder;
        _codeSequenceGenerator = codeSequenceGenerator;
        _tenantContextAccessor = tenantContextAccessor;
        _currentUserService = currentUserService;
        _bootstrapOptions = bootstrapOptions.Value;
        _logger = logger;
    }

    public async Task<TenantBootstrapResult> BootstrapAsync(
        CreateUpdateTenant command,
        CancellationToken cancellationToken)
    {
        var normalizedTenantName = command.TenantName.Trim();
        var normalizedTenantKey = command.TenantKey.Trim().ToLowerInvariant();

        using var tenantFilterBypass = _tenantContextAccessor.BeginFilterBypass();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await EnsureTenantDoesNotExistAsync(normalizedTenantName, normalizedTenantKey, cancellationToken);

        var tenant = CreateTenantAggregate(command, normalizedTenantName, normalizedTenantKey);

        var nextTenantCode = await _codeSequenceGenerator
            .NextTenantCodeAsync(_currentUserService.TenantId, cancellationToken);

        tenant.GenerateTenantCode(nextTenantCode);

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tenantPermissions = await SeedPermissionsAsync(tenant.Id, cancellationToken);
        var adminRole = await CreateAdministratorRoleAsync(tenant.Id, tenantPermissions, cancellationToken);

        var defaultClientId = ResolveDefaultClientId(normalizedTenantKey);

        await CreateDefaultClientAsync(tenant.Id, defaultClientId, cancellationToken);

        var temporaryPassword = ResolveTemporaryPassword();
        var adminUser = await CreateAdminUserAsync(
            tenant.Id,
            adminRole.Id,
            command,
            temporaryPassword,
            cancellationToken);

        await SeedDefaultConfigurationsAsync(tenant.Id, cancellationToken);

        tenant.MarkProvisioned();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInfo(
            "Tenant bootstrap completed. TenantId={TenantId}, TenantKey={TenantKey}, AdminUserId={AdminUserId}",
            tenant.Id,
            tenant.TenantKey,
            adminUser.Id);

        return new TenantBootstrapResult(
            tenant.Id,
            tenant.TenantKey,
            adminUser.Id,
            adminUser.UserName,
            temporaryPassword,
            defaultClientId);
    }

    private async Task EnsureTenantDoesNotExistAsync(
        string tenantName,
        string tenantKey,
        CancellationToken cancellationToken)
    {
        var tenantNameExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => !t.IsDeleted && t.TenantName == tenantName, cancellationToken);

        if (tenantNameExists)
        {
            throw new InvalidOperationException("Tenant name already exists.");
        }

        var tenantKeyExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => !t.IsDeleted && t.TenantKey == tenantKey, cancellationToken);

        if (tenantKeyExists)
        {
            throw new InvalidOperationException("Tenant key already exists.");
        }
    }

    private static Tenant CreateTenantAggregate(
        CreateUpdateTenant command,
        string normalizedTenantName,
        string normalizedTenantKey)
    {
        var authSettings = TenantAuthSetting.Create(0);
        authSettings.SetAuthenticationMode(command.AuthSettings.AuthenticationMode);

        if (command.AuthSettings.AllowLocalLogin) authSettings.EnableLocalLogin();
        else authSettings.DisableLocalLogin();

        if (command.AuthSettings.RequireEmailVerification) authSettings.RequireVerifiedEmail();
        else authSettings.AllowUnverifiedEmail();

        if (command.AuthSettings.AllowSelfRegistration) authSettings.EnableSelfRegistration();
        else authSettings.DisableSelfRegistration();

        if (command.AuthSettings.TwoFactorEnabled)
        {
            authSettings.EnableTwoFactor(TimeSpan.FromMinutes(command.AuthSettings.TwoFactorCodeExpiry ?? 5));
        }
        else
        {
            authSettings.DisableTwoFactor();
        }

        var uiSettings = TenantUISetting.Create(
            command.UISetting?.Theme ?? "Light",
            command.UISetting?.LogoUrl ?? string.Empty,
            command.UISetting?.PrimaryColor ?? "default",
            command.UISetting?.DefaultLanguage ?? "en",
            command.UISetting?.LoginText ?? string.Empty);

        var createResult = Tenant.Create(
            tenantName: normalizedTenantName,
            tenantKey: normalizedTenantKey,
            email: command.Email?.Trim(),
            isActive: command.IsActive,
            authSetting: authSettings,
            tenantUISetting: uiSettings,
            isSystemTenant: false,
            out var tenant);

        if (!createResult.IsSuccess || tenant is null)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Message)));
        }

        return tenant;
    }

    private async Task<List<Permission>> SeedPermissionsAsync(int tenantId, CancellationToken cancellationToken)
    {
        var permissions = DefaultPermissions.CreateDefaultPermissions(
            tenantId,
            includeSystemTenantPermissions: false);

        foreach (var permission in permissions)
        {
            await _permissionSeeder.CreateAsync(_dbContext, tenantId, permission, cancellationToken);
        }

        return await _dbContext.Permissions
            .Where(x => x.TenantId == tenantId && x.ParentId == null)
            .Include(x => x.Children)
            .ThenInclude(x => x.Children)
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
    }

    private async Task<Role> CreateAdministratorRoleAsync(
        int tenantId,
        IReadOnlyCollection<Permission> tenantPermissions,
        CancellationToken cancellationToken)
    {
        var tenantPermissionLookup = FlattenPermissions(tenantPermissions)
            .ToDictionary(x => x.PermissionKey, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var templatePermissions = await ResolveSystemAdministratorPermissionsAsync(cancellationToken);
        if (templatePermissions.Count == 0)
        {
            templatePermissions = DefaultRoles
                .CreateRole(tenantPermissions.ToList())
                .RolePermissions
                .Where(permission => !IsSystemOnlyTenantPermission(permission.PermissionKey))
                .ToList();
        }

        var rolePermissions = templatePermissions
            .Where(permission => !IsSystemOnlyTenantPermission(permission.PermissionKey))
            .Where(permission => tenantPermissionLookup.ContainsKey(permission.PermissionKey))
            .Select(permission => new CreateUpdateRolePermission
            {
                PermissionId = tenantPermissionLookup[permission.PermissionKey],
                RoleId = 0,
                PermissionKey = permission.PermissionKey,
                IsAllowed = permission.IsAllowed
            })
            .ToList();

        var roleCommand = new CreateUpdateRole
        {
            RoleName = "Administrator",
            RoleDescription = "Tenant administrator",
            IsActive = true,
            RolePermissions = rolePermissions
        };

        return await _roleProvisioningService.CreateAsync(_dbContext, tenantId, roleCommand, cancellationToken);
    }

    private async Task<List<CreateUpdateRolePermission>> ResolveSystemAdministratorPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var systemTenantId = _currentUserService.TenantId;
        if (systemTenantId <= 0)
        {
            return new List<CreateUpdateRolePermission>();
        }

        var administratorRole = await _dbContext.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(
                x => x.TenantId == systemTenantId &&
                     !x.IsDeleted &&
                     x.Name == "Administrator",
                cancellationToken);

        if (administratorRole is null)
        {
            return new List<CreateUpdateRolePermission>();
        }

        return administratorRole.RolePermissions
            .Select(permission => new CreateUpdateRolePermission
            {
                PermissionId = permission.PermissionId,
                RoleId = 0,
                PermissionKey = permission.PermissionKey,
                IsAllowed = permission.IsAllowed
            })
            .ToList();
    }

    private async Task CreateDefaultClientAsync(
        int tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var clientCommand = DefaultClients.GetDefaultClient(
            _bootstrapOptions.RedirectUri,
            _bootstrapOptions.LogoutRedirectUri);

        await _clientProvisioningService.CreateAsync(
            _dbContext,
            tenantId,
            clientId,
            clientCommand,
            cancellationToken);
    }

    private string ResolveDefaultClientId(string tenantKey)
    {
        var configuredClientId = _bootstrapOptions.ClientId?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredClientId) &&
            !SystemIdentity.IsReservedSystemClientId(configuredClientId))
        {
            return configuredClientId;
        }

        return SystemIdentity.GetDefaultOperationalAdminClientId(tenantKey);
    }

    private async Task<User> CreateAdminUserAsync(
        int tenantId,
        int administratorRoleId,
        CreateUpdateTenant command,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        var adminEmail = ResolveAdminEmail(command);

        var adminUser = new UserDetail
        {
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            Email = adminEmail,
            FirstName = command.AdminFirstName.Trim(),
            LastName = command.AdminLastName.Trim(),
            Phone = "0000000000",
            Status = "Active",
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            Password = temporaryPassword,
            Roles = new[] { administratorRoleId }
        };

        return await _userProvisioningService.CreateAsync(
            _dbContext,
            tenantId,
            adminUser,
            cancellationToken);
    }

    private static string ResolveAdminEmail(CreateUpdateTenant command)
    {
        var adminEmail = !string.IsNullOrWhiteSpace(command.AdminEmail)
            ? command.AdminEmail
            : command.Email;

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new InvalidOperationException("Admin email must not be empty.");
        }

        return adminEmail.Trim();
    }

    private async Task SeedDefaultConfigurationsAsync(int tenantId, CancellationToken cancellationToken)
    {
        foreach (var configuration in DefaultConfigurations.Notification)
        {
            await _configurationSeeder.CreateAsync(_dbContext, tenantId, new CreateUpdateConfiguration
            {
                ConfigKey = configuration.Key,
                ConfigValue = configuration.Value,
                ValueType = configuration.ValueType,
                Scope = configuration.Scope,
                IsEditable = configuration.isEditable
            }, cancellationToken);
        }
    }

    private string ResolveTemporaryPassword()
    {
        if (!string.IsNullOrWhiteSpace(_bootstrapOptions.AdminTempPassword))
        {
            return _bootstrapOptions.AdminTempPassword.Trim();
        }

        return $"Tmp!{Guid.NewGuid():N}"[..16];
    }

    private static IEnumerable<Permission> FlattenPermissions(IEnumerable<Permission> permissions)
    {
        foreach (var permission in permissions)
        {
            yield return permission;

            foreach (var child in FlattenPermissions(permission.Children))
            {
                yield return child;
            }
        }
    }

    private static bool IsSystemOnlyTenantPermission(string permissionKey)
    {
        return permissionKey.Equals("tenants.add", StringComparison.OrdinalIgnoreCase)
            || permissionKey.Equals("tenants.delete", StringComparison.OrdinalIgnoreCase);
    }
}
