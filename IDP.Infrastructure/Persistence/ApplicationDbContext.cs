using IDP.Domain.AggregateRoots;
using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Outbox;
using IDP.Domain.AggregateRoots.Permissions;
using IDP.Domain.AggregateRoots.Tokens;
using IDP.Domain.ReadModels;
using IDP.Infrastructure.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IDP.Infrastructure.Persistence;

internal partial class ApplicationDbContext : IdentityDbContext<User, Role, int>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ApplicationDbContext> _appLogger;
    private readonly IOutboxMapperResolver _resolver;
    private readonly IOutboxConsumerRouter _consumerRouter;
    public ApplicationDbContext(DbContextOptions options,
        ICurrentUserService currentUserService,
        IAppLogger<ApplicationDbContext> appLogger,
        IOutboxMapperResolver resolver,
        IOutboxConsumerRouter consumerRouter) : base(options)
    {
        _currentUserService = currentUserService;
        _appLogger = appLogger;
        _resolver = resolver;
        _consumerRouter = consumerRouter;
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
    public DbSet<OutboxEventConsumer> OutboxEventConsumers { get; set; }
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
    public DbSet<TokenReadModel> TokenReadModel { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<DashboardMetric> DashboardMetrics { get; set; }
    public DbSet<DashboardMetricsCheckpoint> DashboardMetricsCheckpoints { get; set; }
    public DbSet<DashboardMetricRanking> DashboardMetricRankings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Save changes in database async
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>affected rows</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        SetAuditFields();

        var aggregateEvents = ChangeTracker.Entries<IAggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var applicationEvents = DomainEvents.ToList();

        var allEvents = aggregateEvents.Concat(applicationEvents).ToList();

        foreach (var evt in allEvents)
        {
            var outbox = _resolver.Resolve(evt);

            var consumers = _consumerRouter.ResolveConsumers(evt);

            foreach (var consumer in consumers)
            {
                outbox.AddConsumer(consumer);
            }

            OutboxEvents.Add(outbox);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }

        ClearDomainEvents();

        return result;
    }

    /// <summary>
    /// set table audit fields
    /// </summary>
    /// <param name="entries"></param>
    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<IAggregateRoot>().ToList();

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

internal partial class ApplicationDbContext
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void AddDomainEvent(IDomainEvent evt)
        => _domainEvents.Add(evt);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}