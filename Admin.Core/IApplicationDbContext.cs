namespace Admin.Core;

public interface IApplicationDbContext
{
    DbSet<AppConfiguration> AppConfigurations { get; set; }
    DbSet<AppUser> AppUsers { get; set; }
    DbSet<AppClaim> AppClaims { get; set; }
    DbSet<AppClaimTenant> AppClaimTenants { get; set; }
    DbSet<AppRole> AppRoles { get; set; }
    DbSet<AppRoleClaim> AppRoleClaims { get; set; }
    DbSet<AppUserRole> AppUserRoles { get; set; }
    DbSet<Tenant> Tenants { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<UserSearch> UsersSearch { get; set; }
    DbSet<UserClaim> UsersClaims { get; set; }
    DbSet<RoleSearch> RolesSearch { get; set; }
    DbSet<TenantSearch> TenantsSearch { get; set; }
    DbSet<ReportSearch> ReportsSearch { get; set; }
    DbSet<ConfigurationSearch> ConfigurationsSearch { get; set; }
    DbSet<StateLookup> StateLookups { get; set; }

    ChangeTracker ChangeTracker { get; }

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

