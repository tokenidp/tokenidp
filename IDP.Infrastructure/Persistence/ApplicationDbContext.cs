using IDP.Domain.AggregateRoots;
using IDP.Domain.AggregateRoots.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IDP.Infrastructure.Persistence;

internal sealed class ApplicationDbContext : IdentityDbContext<
    User,
    Role,
    int,
    IdentityUserClaim<int>,
    UserRole,
    IdentityUserLogin<int>,
    RolePermission,
    IdentityUserToken<int>>, IApplicationDbContext
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
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PreAuthorization> PreAuthorizations { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<ClientAudience> ClientAudiences { get; set; }
    public DbSet<ClientSecret> ClientSecrets { get; set; }
    public DbSet<ClientGrantType> ClientGrantTypes { get; set; }
    public DbSet<ReferenceToken> ReferenceTokens { get; set; }
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

        builder.Entity<PreAuthorization>().ToTable("PreAuthorizations");

        builder.Entity<AuthorizationCode>().ToTable("AuthorizationCodes");

        builder.Entity<RefreshToken>().ToTable("RefreshTokens");

        builder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");

            entity.Property(p => p.TokenType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<TokenTypes>(v));

            entity.Property(p => p.ClientType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ClientTypes>(v));

            entity.Property(p => p.AppType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<AppTypes>(v));
        });

        builder.Entity<ClientScope>().ToTable("ClientScopes");

        builder.Entity<ClientGrantType>(entity =>
        {
            entity.ToTable("ClientGrantTypes");

            entity.Property(p => p.AllowedGrantType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<GrantTypes>(v));
        });

        builder.Entity<ReferenceToken>().ToTable("UserAccessTokens");

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
