using Admin.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Reflection;

namespace Identity.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<
    AppUser,
    AppRole,
    int,
    IdentityUserClaim<int>,
    AppUserRole,
    IdentityUserLogin<int>,
    AppRoleClaim,
    IdentityUserToken<int>>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public ApplicationDbContext(DbContextOptions options,
        ICurrentUserService currentUserService,
        IAuditService auditService) : base(options)
    {
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public DbSet<AppConfiguration> AppConfigurations { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AppClaim> AppClaims { get; set; }
    public DbSet<AppClaimTenant> AppClaimTenants { get; set; }
    public DbSet<AppRole> AppRoles { get; set; }
    public DbSet<AppRoleClaim> AppRoleClaims { get; set; }
    public DbSet<AppUserRole> AppUserRoles { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserSearch> UsersSearch { get; set; }
    public DbSet<UserClaim> UsersClaims { get; set; }
    public DbSet<RoleSearch> RolesSearch { get; set; }
    public DbSet<TenantSearch> TenantsSearch { get; set; }
    public DbSet<ReportSearch> ReportsSearch { get; set; }
    public DbSet<ConfigurationSearch> ConfigurationsSearch { get; set; }
    public DbSet<StateLookup> StateLookups { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<AppUserRole>(ur =>
        {
            ur.ToTable("AppUserRole").HasKey(s => s.Id);
        });

        builder.Entity<AppRoleClaim>().ToTable("AppRoleClaim");

        builder.Entity<IdentityUserClaim<int>>().ToTable("AppUserClaim");

        builder.Entity<IdentityUserLogin<int>>().ToTable("AppUserLogin");

        builder.Entity<IdentityUserToken<int>>().ToTable("AppUserToken");
    }

    /// <summary>
    /// Save changes in database
    /// </summary>
    /// <returns>affected rows</returns>
    public override int SaveChanges()
    {
        SetAuditFields();

        _auditService.CreateAuditLog(this);

        var result = base.SaveChanges();

        return result;
    }

    /// <summary>
    /// Save changes in database async
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>affected rows</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        SetAuditFields();

        _auditService.CreateAuditLog(this);

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// set table audit fields
    /// </summary>
    /// <param name="entries"></param>
    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<IBaseEntity>().ToList();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:

                    entry.Entity.SetCreatedByAndCreatedOn(_currentUserService.UserId);
                    break;

                case EntityState.Modified:

                    entry.Property(x => x.CreatedBy).IsModified = false;
                    entry.Property(x => x.CreatedOn).IsModified = false;

                    entry.Entity.SetUpdatedByAndUpdatedOn(_currentUserService.UserId);
                    break;
            }
        }
    }
}
