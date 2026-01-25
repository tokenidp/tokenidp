using IDP.Domain.AggregateRoots;
using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Permissions;
using IDP.Domain.AggregateRoots.Tokens;
using IDP.Infrastructure.Outbox;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IDP.Infrastructure.Persistence;

internal sealed class ApplicationDbContext : IdentityDbContext<User, Role, int>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(DbContextOptions options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<PreAuthorization> PreAuthorizations { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<ClientAudience> ClientAudiences { get; set; }
    public DbSet<ClientSecret> ClientSecrets { get; set; }
    public DbSet<ClientGrantType> ClientGrantTypes { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ReferenceToken> ReferenceTokens { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<LookupType> LookupTypes { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<UserRolePermission> UserRolePermissions { get; set; }
    public DbSet<UserSearch> UsersSearch { get; set; }
    public DbSet<RoleSearch> RolesSearch { get; set; }
    public DbSet<TenantSearch> TenantsSearch { get; set; }
    public DbSet<ConfigurationSearch> ConfigurationsSearch { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<UserContact> UserContacts { get; set; }
    public DbSet<TokenSearch> TokenSearch { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Save changes in database
    /// </summary>
    /// <returns>affected rows</returns>
    public override int SaveChanges()
    {
        SetAuditFields();

        var domainEvents = ChangeTracker.Entries<AggregateRoot<Guid>>().SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var evt in domainEvents)
        {
            var outbox = DomainEventOutboxMapper.Map(evt);
            OutboxEvents.Add(outbox);
        }

        var result = base.SaveChanges();

        foreach (var entry in ChangeTracker.Entries<AggregateRoot<Guid>>())
        {
            entry.Entity.ClearDomainEvents();
        }

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

        var domainEvents = ChangeTracker.Entries<AggregateRoot<Guid>>().SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var evt in domainEvents)
        {
            var outbox = DomainEventOutboxMapper.Map(evt);
            OutboxEvents.Add(outbox);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in ChangeTracker.Entries<AggregateRoot<Guid>>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return result;
    }

    /// <summary>
    /// set table audit fields
    /// </summary>
    /// <param name="entries"></param>
    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditableAggregate>().ToList();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:

                    entry.Entity.SetCreated(_currentUserService.UserId);
                    break;

                case EntityState.Modified:

                    entry.Property(x => x.CreatedBy).IsModified = false;
                    entry.Property(x => x.CreatedOn).IsModified = false;

                    entry.Entity.SetUpdated(_currentUserService.UserId);
                    break;
            }
        }
    }
}
