using Admin.Core.Bootstrap;
using Admin.Core.Configurations;
using IDP.Domain.AggregateRoots.Permissions;
using IDP.Infrastructure.Bootstrap.SeedData;
using IDP.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace IDP.Infrastructure.Bootstrap;

internal class SystemBootstrapper : ISystemBootstrapper
{
    private readonly ITenantProvisioningService _tenants;
    private readonly IClientProvisioningService _clients;
    private readonly IRoleProvisioningService _roles;
    private readonly IUserProvisioningService _users;
    private readonly IPermissionSeeder _permissions;
    private readonly IConfigurationSeeder _configs;
    private readonly IAppLogger<SystemBootstrapper> _logger;
    private readonly BootstrapOptions _bootstrapOptions;

    public SystemBootstrapper(ITenantProvisioningService tenants,
        IClientProvisioningService clients,
        IUserProvisioningService users,
        IRoleProvisioningService roles,
        IPermissionSeeder permissions,
        IConfigurationSeeder configs,
        IOptions<BootstrapOptions> options,
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

    public async Task BootstrapAsync(CancellationToken ct, string connectionStringName)
    {
        _logger.LogInfo("IDP Bootstrap started...");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionStringName)
            .Options;

        var systemUser = new SystemCurrentUserService();

        using (var db = new ApplicationDbContext(options, systemUser))
        {
            var systemTenant = await EnsureSystemTenantAsync(db, ct);

            await EnsureAdminClientAsync(db, systemTenant.Id, ct);

            var permissions = await EnsurePermissionsAsync(db, systemTenant.Id, ct);

            var role = await EnsureDefaultRolesAsync(db, systemTenant.Id, permissions, ct);

            var user = await EnsureDefaultAdminUserAsync(db, systemTenant.Id, role.Id, ct);

            await EnsureConfigurationsAsync(db, systemTenant.Id, ct);

            _logger.LogInfo("IDP Bootstrap completed.");
        }
    }

    private async Task<Tenant> EnsureSystemTenantAsync(IApplicationDbContext db,
        CancellationToken ct)
    {
        var defaultTenant = DefaultTenants.SystemTenant;
        var existing = await _tenants.ExistsAsync(db, defaultTenant.TenantCode, ct);

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

        if (await _clients.ExistsAsync(db, tenantId, adminClientId, ct))
        {
            _logger.LogInfo("Admin client already exists.");
            return;
        }

        _logger.LogInfo("Creating Admin client...");

        await _clients.CreateAsync(db, tenantId,
            DefaultClients.GetDefaultClient(_bootstrapOptions.RedirectUri,
            _bootstrapOptions.LogoutRedirectUri),
            ct);

        _logger.LogInfo("Admin client created.");
    }

    private async Task<Role> EnsureDefaultRolesAsync(IApplicationDbContext db,
        int tenantId,
        List<Permission> permissions,
        CancellationToken ct)
    {
        const string defaultRoleName = "Administrator";

        if (await _roles.ExistsAsync(db, tenantId, defaultRoleName, ct))
            return default!;

        _logger.LogInfo("Creating Admin role...");

        var defaultRole = DefaultRoles.CreateRole(permissions);

        var role = await _roles.CreateAsync(db, tenantId, defaultRole, ct);

        _logger.LogInfo("Admin role created.");

        return role;
    }

    private async Task<User> EnsureDefaultAdminUserAsync(IApplicationDbContext db,
        int tenantId,
        int adminRoleId,
        CancellationToken ct)
    {
        const string adminUserName = "ADMIN";

        var existing = await _users.ExistsAsync(db, tenantId, adminUserName, ct);
        if (existing != null)
        {
            _logger.LogInfo("Admin user already exists.");
            return default!;
        }

        var tempPassword = _bootstrapOptions.AdminTempPassword;

        if (string.IsNullOrWhiteSpace(tempPassword))
            throw new InvalidOperationException(
                "Bootstrap AdminTempPassword is not configured.");

        var adminUser = DefaultUsers.Admin(tempPassword);

        adminUser.Roles.Append(adminRoleId);

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

        List<Permission> createdPermissions = new();

        foreach (var permission in permissions)
        {
            if (await _permissions.ExistsAsync(db, tenantId, permission.PermissionKey, ct))
                continue;

            _logger.LogInfo("Creating permission...");

            var createdPermission = await _permissions.CreateAsync(db, tenantId, permission, ct);

            createdPermissions.Add(createdPermission);

            _logger.LogInfo("Permission created.");
        }

        return createdPermissions;
    }
}