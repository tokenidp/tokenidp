using IDP.Core.Domain.AggregateRoots;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IDP.Core.Infrastructure;

internal class ApplicationDbContext : IdentityDbContext<
    User,
    Role,
    int,
    IdentityUserClaim<int>,
    UserRole,
    IdentityUserLogin<int>,
    RolePermission,
    IdentityUserToken<int>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly AuditService _auditService;

    public ApplicationDbContext(DbContextOptions options,
        ICurrentUserService currentUserService,
        AuditService auditService) : base(options)
    {
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<TenantPermission> TenantPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserRefreshToken> RefreshTokens { get; set; }
    public DbSet<UserPreAuthorization> PreAuthorizations { get; set; }
    public DbSet<UserAuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<ClientAudience> ClientAudiences { get; set; }
    public DbSet<ClientSecret> ClientSecrets { get; set; }
    public DbSet<ClientGrantType> ClientGrantTypes { get; set; }
    public DbSet<UserAccessToken> UserAccessToken { get; set; }
    public DbSet<LookupType> LookupTypes { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserRolePermission> UserRolePermissions { get; set; }
    public DbSet<UserSearch> UsersSearch { get; set; }
    public DbSet<RoleSearch> RolesSearch { get; set; }
    public DbSet<TenantSearch> TenantsSearch { get; set; }
    public DbSet<ConfigurationSearch> ConfigurationsSearch { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<RolePermission>().ToTable("RolePermissions");

        builder.Entity<UserRole>().ToTable("UserRoles");

        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");

        builder.Entity<UserPreAuthorization>().ToTable("PreAuthorizations");

        builder.Entity<UserAuthorizationCode>().ToTable("AuthorizationCodes");

        builder.Entity<UserRefreshToken>().ToTable("RefreshTokens");

        builder.Entity<Client>(ur =>
        {
            ur.ToTable("Clients").Property(p => p.AccessTokenType)
               .HasConversion(
                v => v.ToString(),
                v => (TokenType)Enum.Parse(typeof(TokenType), v));
        });

        builder.Entity<ClientScope>().ToTable("ClientScopes");

        builder.Entity<UserAccessToken>().ToTable("UserAccessTokens");

        builder.Entity<LookupType>().ToTable("LookupTypes");

        builder.Entity<LookupValue>().ToTable("LookupValues");
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

        //_auditService.CreateAuditLog(this);

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
