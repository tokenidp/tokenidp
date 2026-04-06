using TokenIDP.Core.Admin.Bootstrap;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Core.Admin.Roles;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Infrastructure.Bootstrap.SeedData;
using TokenIDP.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class SystemBootstrapper : ISystemBootstrapper
{
    private readonly ITenantProvisioningService _tenants;
    private readonly IClientProvisioningService _clients;
    private readonly IRoleProvisioningService _roles;
    private readonly IUserProvisioningService _users;
    private readonly IPermissionSeeder _permissions;
    private readonly IConfigurationSeeder _configs;
    private readonly IAppLogger<SystemBootstrapper> _logger;
    private readonly BootstrapOption _bootstrapOptions;

    public SystemBootstrapper(ITenantProvisioningService tenants,
        IClientProvisioningService clients,
        IUserProvisioningService users,
        IRoleProvisioningService roles,
        IPermissionSeeder permissions,
        IConfigurationSeeder configs,
        IOptions<BootstrapOption> options,
        IAppLogger<SystemBootstrapper> logger)
    {
        _tenants = tenants;
        _clients = clients;
        _users = users;
        _roles = roles;
        _permissions = permissions;
        _configs = configs;
        _logger = logger;
        _bootstrapOptions = options.Value;
    }

    public async Task BootstrapAsync(CancellationToken ct, string databaseProvider, string connectionString)
    {
        _logger.LogInfo("IDP Bootstrap started...");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseApplicationDatabase(databaseProvider, connectionString)
            .Options;

        var systemUser = new SystemCurrentUserService();

        using (var db = new ApplicationDbContext(options, systemUser))
        {
            var systemTenant = await EnsureSystemTenantAsync(db, ct);

            if (systemTenant == null)
                return;

            await EnsureAdminApiResourceAsync(db, systemTenant.Id, ct);

            await EnsureAdminClientAsync(db, systemTenant.Id, ct);

            var permissions = await EnsurePermissionsAsync(db, systemTenant.Id, ct);

            if (permissions == null)
                return;

            var role = await EnsureDefaultRolesAsync(db, systemTenant.Id, permissions, ct);

            if (role == null) return;

            var user = await EnsureDefaultAdminUserAsync(db, systemTenant.Id, role.Id, ct);

            if (user == null) return;

            await EnsureConfigurationsAsync(db, systemTenant.Id, ct);

            _logger.LogInfo("IDP Bootstrap completed.");
        }
    }

    private async Task<Tenant> EnsureSystemTenantAsync(IApplicationDbContext db,
        CancellationToken ct)
    {
        var defaultTenant = DefaultTenants.SystemTenant;

        var tenantCode = defaultTenant.GenerateTenantCode(1);

        var existing = await _tenants.ExistsAsync(db, tenantCode, ct);

        if (existing != null)
        {
            _logger.LogInfo("System tenant already exists.");
            return existing;
        }

        _logger.LogInfo("Creating system tenant...");

        var tenant = await _tenants.CreateSystemTenantAsync(db, defaultTenant, ct);

        _logger.LogInfo("System tenant created with Id {TenantId}", tenant.Id);

        return tenant;
    }

    private async Task EnsureAdminClientAsync(IApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        const string adminClientId = "idp-admin";
        var defaultAdminClient = DefaultClients.GetDefaultClient(
            _bootstrapOptions.RedirectUri,
            _bootstrapOptions.LogoutRedirectUri);

        var existing = await db.Clients
            .Include(x => x.ClientScopes)
            .Include(x => x.ClientApiResources)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientId == adminClientId, ct);

        if (existing != null)
        {
            var clientUpdated = false;

            var existingScopes = existing.ClientScopes
                .Select(x => x.Scope)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var updatedScopes = existing.ClientScopes.ToList();

            foreach (var scopeName in defaultAdminClient.Scopes.Where(scope => !existingScopes.Contains(scope)))
            {
                updatedScopes.Add(CreateClientScope(scopeName));
                clientUpdated = true;
            }

            if (clientUpdated)
            {
                existing.ReplaceScopes(updatedScopes);
            }

            var existingApiResources = existing.ClientApiResources
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var updatedApiResources = existing.ClientApiResources.ToList();

            foreach (var apiResourceName in defaultAdminClient.ApiResources.Where(resource => !existingApiResources.Contains(resource)))
            {
                updatedApiResources.Add(CreateClientApiResource(apiResourceName));
                clientUpdated = true;
            }

            if (clientUpdated)
            {
                existing.ReplaceApiResources(updatedApiResources);
                await db.SaveChangesAsync(ct);
                _logger.LogInfo("Admin client defaults updated.");
                return;
            }

            _logger.LogInfo("Admin client already exists.");
            return;
        }

        _logger.LogInfo("Creating Admin client...");

        await _clients.CreateAsync(db, tenantId, adminClientId,
            defaultAdminClient,
            ct);

        _logger.LogInfo("Admin client created.");
    }

    private async Task EnsureAdminApiResourceAsync(IApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var defaultApiResource = DefaultApiResources.AdminApi;

        var existing = await db.ApiResources
            .Include(x => x.Scopes)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == defaultApiResource.Name, ct);

        if (existing == null)
        {
            _logger.LogInfo("Creating Admin API resource...");

            var createdResource = CreateApiResource(tenantId, defaultApiResource);
            createdResource.ReplaceScopes(defaultApiResource.Scopes
                .Select(CreateApiScope)
                .ToList());

            db.ApiResources.Add(createdResource);
            await db.SaveChangesAsync(ct);

            _logger.LogInfo("Admin API resource created.");
            return;
        }

        var existingScopeNames = existing.Scopes
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingScopes = defaultApiResource.Scopes
            .Where(scope => !existingScopeNames.Contains(scope.Name))
            .ToList();

        if (missingScopes.Count == 0)
        {
            _logger.LogInfo("Admin API resource already exists.");
            return;
        }

        var updatedScopes = existing.Scopes.ToList();
        updatedScopes.AddRange(missingScopes.Select(CreateApiScope));
        existing.ReplaceScopes(updatedScopes);

        await db.SaveChangesAsync(ct);

        _logger.LogInfo(
            "Admin API resource updated with {ScopeCount} missing scope(s).",
            missingScopes.Count);
    }

    private async Task<Role> EnsureDefaultRolesAsync(IApplicationDbContext db,
        int tenantId,
        List<Permission> permissions,
        CancellationToken ct)
    {
        const string defaultRoleName = "Administrator";
        var defaultRole = DefaultRoles.CreateRole(permissions);

        var existingRole = await db.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == defaultRoleName, ct);

        if (existingRole != null)
        {
            var addedPermissions = await EnsureRolePermissionsAsync(db, existingRole, defaultRole.RolePermissions, ct);

            if (addedPermissions > 0)
            {
                _logger.LogInfo(
                    "Admin role already exists. Added {PermissionCount} missing permission(s).",
                    addedPermissions);
            }
            else
            {
                _logger.LogInfo("Admin role already exists.");
            }

            return existingRole;
        }

        _logger.LogInfo("Creating Admin role...");

        var role = await _roles.CreateAsync(db, tenantId, defaultRole, ct);

        _logger.LogInfo("Admin role created.");

        return role;
    }

    private async Task<User> EnsureDefaultAdminUserAsync(IApplicationDbContext db,
        int tenantId,
        int adminRoleId,
        CancellationToken ct)
    {
        string adminUserName = _bootstrapOptions.AdminName;

        var existing = await _users.ExistsAsync(db, tenantId, adminUserName, ct);
        if (existing != null)
        {
            _logger.LogInfo("Admin user already exists.");
            return existing;
        }

        var tempPassword = _bootstrapOptions.AdminTempPassword;

        if (string.IsNullOrWhiteSpace(tempPassword))
            throw new InvalidOperationException(
                "Bootstrap AdminTempPassword is not configured.");

        var adminUser = DefaultUsers.Admin(adminUserName, tempPassword);

        adminUser.Roles = adminUser.Roles.Append(adminRoleId).ToArray();

        _logger.LogInfo("Creating Admin user...");

        var created = await _users.CreateAsync(db, tenantId, adminUser, ct);

        _logger.LogInfo("Admin user created.");

        return created;
    }

    private async Task EnsureConfigurationsAsync(IApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        foreach (var cfg in DefaultConfigurations.Notification)
        {
            if (await _configs.ExistsAsync(db, tenantId, cfg.Key, cfg.Scope.ToString(), ct))
                continue;

            _logger.LogInfo("Creating configuration...");

            await _configs.CreateAsync(db, tenantId, new CreateUpdateConfiguration
            {
                ConfigKey = cfg.Key,
                ConfigValue = cfg.Value,
                ValueType = cfg.ValueType,
                Scope = cfg.Scope,
                IsEditable = cfg.isEditable
            }, ct);

            _logger.LogInfo("Configuration created.");
        }
    }

    private async Task<List<Permission>> EnsurePermissionsAsync(IApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var permissions = DefaultPermissions.CreateDefaultPermissions(tenantId);

        foreach (var permission in permissions)
        {
            if (await _permissions.ExistsAsync(db, tenantId, permission.PermissionKey, ct))
                continue;

            _logger.LogInfo("Creating permission...");

            await _permissions.CreateAsync(db, tenantId, permission, ct);

            _logger.LogInfo("Permission created.");
        }

        return await db.Permissions
            .Where(x => x.TenantId == tenantId && x.ParentId == null)
            .Include(x => x.Children)
            .ThenInclude(x => x.Children)
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);
    }

    private static async Task<int> EnsureRolePermissionsAsync(
        IApplicationDbContext db,
        Role role,
        IEnumerable<CreateUpdateRolePermission> permissions,
        CancellationToken ct)
    {
        var existingKeys = role.RolePermissions
            .Select(x => x.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedPermissions = 0;

        foreach (var permission in permissions)
        {
            if (existingKeys.Contains(permission.PermissionKey))
            {
                continue;
            }

            var result = role.AddPermission(
                tenantPermissionId: permission.PermissionId,
                permissionKey: permission.PermissionKey,
                isAllowed: permission.IsAllowed,
                bypassEditableCheck: true);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to provision role permission '{permission.PermissionKey}': {FormatErrors(result)}");
            }

            existingKeys.Add(permission.PermissionKey);
            addedPermissions++;
        }

        if (addedPermissions > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return addedPermissions;
    }

    private static ApiResource CreateApiResource(
        int tenantId,
        DefaultApiResourceDefinition definition)
    {
        var result = ApiResource.Create(
            tenantId,
            definition.Name,
            definition.DisplayName,
            definition.Description,
            definition.Enabled,
            out var apiResource);

        if (!result.IsSuccess || apiResource == null)
        {
            throw new InvalidOperationException(
                $"Failed to create default ApiResource {definition.Name}: {FormatErrors(result)}");
        }

        return apiResource;
    }

    private static ApiScope CreateApiScope(DefaultApiScopeDefinition definition)
    {
        var result = ApiScope.Create(
            definition.Name,
            definition.DisplayName,
            definition.Description,
            definition.Enabled,
            out var apiScope);

        if (!result.IsSuccess || apiScope == null)
        {
            throw new InvalidOperationException(
                $"Failed to create default ApiScope {definition.Name}: {FormatErrors(result)}");
        }

        return apiScope;
    }

    private static ClientScope CreateClientScope(string scopeName)
    {
        var result = ClientScope.Create(scopeName, out var clientScope);

        if (!result.IsSuccess || clientScope == null)
        {
            throw new InvalidOperationException(
                $"Failed to create default client scope {scopeName}: {FormatErrors(result)}");
        }

        return clientScope;
    }

    private static ClientApiResource CreateClientApiResource(string apiResourceName)
    {
        var result = ClientApiResource.Create(apiResourceName, true, out var clientApiResource);

        if (!result.IsSuccess || clientApiResource == null)
        {
            throw new InvalidOperationException(
                $"Failed to create default client api resource {apiResourceName}: {FormatErrors(result)}");
        }

        return clientApiResource;
    }

    private static string FormatErrors(Result result)
    {
        return string.Join("; ", result.Errors.Select(x => x.Message));
    }
}

