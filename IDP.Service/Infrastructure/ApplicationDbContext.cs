using IDP.Service.Domain.ComplexTypes;
using static IDP.Service.Domain.User;

namespace IDP.Service.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<
    User,
    Role,
    int,
    IdentityUserClaim<int>,
    UserRole,
    IdentityUserLogin<int>,
    RolePermission,
    IdentityUserToken<int>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserClaim> UsersClaims { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PreAuthorization> PreAuthorizations { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }

    public DbSet<UserAccessToken> UserAccessToken { get; set; }
    public DbSet<LookupType> LookupTypes { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<User>(ur =>
        {
            ur.ToTable("Users").Property(e => e.StatusId)
                .HasConversion(
                    v => v.ToString(),
                    v => (UserStatus)Enum.Parse(typeof(UserStatus), v));
        });

        builder.Entity<Role>(ur =>
        {
            ur.ToTable("Roles").Property(e => e.Name).HasColumnName("RoleName");
        });

        builder.Entity<RolePermission>().ToTable("RolePermissions");

        builder.Entity<UserRole>().ToTable("UserRoles");

        builder.Entity<PreAuthorization>().ToTable("PreAuthorizations");

        builder.Entity<AuthorizationCode>().ToTable("AuthorizationCodes");

        builder.Entity<RefreshToken>().ToTable("RefreshTokens");

        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");

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

        builder.Entity<UserClaim>().ToView("vUserClaims");
    }
}
