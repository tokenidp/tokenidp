using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth;
using TokenIDP.Domain.AggregateRoots;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Configurations;
using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Domain.AggregateRoots.Tokens;
using TokenIDP.Domain.ReadModels;
using TokenIDP.Infrastructure.Outbox.Abstractions;

namespace TokenIDP.Infrastructure.Persistence;

public partial class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IAppLogger<ApplicationDbContext> _appLogger;
    private readonly IOutboxMapperResolver _resolver;
    private readonly IOutboxConsumerRouter _consumerRouter;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        ITenantContextAccessor tenantContextAccessor,
        IAppLogger<ApplicationDbContext> appLogger,
        IOutboxMapperResolver resolver,
        IOutboxConsumerRouter consumerRouter) : base(options)
    {
        _currentUserService = currentUserService;
        _tenantContextAccessor = tenantContextAccessor;
        _appLogger = appLogger;
        _resolver = resolver;
        _consumerRouter = consumerRouter;
    }

    internal ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
        _tenantContextAccessor = new TenantContextAccessor();
        _appLogger = NullAppLogger<ApplicationDbContext>.Instance;
        _resolver = NullOutboxMapperResolver.Instance;
        _consumerRouter = NullOutboxConsumerRouter.Instance;
    }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantAuthSetting> TenantAuthSettings { get; set; }
    public DbSet<TenantExternalProvider> TenantExternalProviders { get; set; }
    public DbSet<TenantUISetting> TenantUISettings { get; set; }
    public DbSet<PreAuthorization> PreAuthorizations { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<DeviceAuthorization> DeviceAuthorizations { get; set; }
    public DbSet<BackchannelAuthenticationRequest> BackchannelAuthenticationRequests { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<ClientApiResource> ClientApiResources { get; set; }
    public DbSet<ApiResource> ApiResources { get; set; }
    public DbSet<ApiScope> ApiScopes { get; set; }
    public DbSet<ClientSecret> ClientSecrets { get; set; }
    public DbSet<ClientGrantType> ClientGrantTypes { get; set; }
    public DbSet<ClientAuthPolicy> ClientAuthPolicies { get; set; }
    public DbSet<ClientExternalProvider> ClientExternalProviders { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ReferenceToken> ReferenceTokens { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<OutboxEventConsumer> OutboxEventConsumers { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<UserRolePermission> UserRolePermissions { get; set; }
    public DbSet<UserSearch> UsersSearch { get; set; }
    public DbSet<RoleSearch> RolesSearch { get; set; }
    public DbSet<TenantSearch> TenantsSearch { get; set; }
    public DbSet<ConfigurationSearch> ConfigurationsSearch { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }
    public DbSet<CodeSequence> CodeSequences { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<UserContact> UserContacts { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }
    public DbSet<TokenSearch> TokenSearch { get; set; }
    public DbSet<TokenReadModel> TokenReadModel { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<DashboardMetric> DashboardMetrics { get; set; }
    public DbSet<DashboardMetricsCheckpoint> DashboardMetricsCheckpoints { get; set; }
    public DbSet<DashboardMetricRanking> DashboardMetricRankings { get; set; }
    public DbSet<EmailMessage> EmailMessages { get; set; }
    public DbSet<EmailDeliveryAttempt> EmailDeliveryAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ApplyTenantQueryFilters(builder);
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

                    entry.Entity.SetCreated(_currentUserService.UserId > 0
                        ? _currentUserService.UserId
                        : entry.Entity.CreatedBy);
                    break;

                case EntityState.Modified:

                    entry.Property(x => x.CreatedBy).IsModified = false;
                    entry.Property(x => x.CreatedAtUtc).IsModified = false;

                    entry.Entity.SetUpdated(_currentUserService.UserId);
                    break;
            }
        }
    }

    private void ApplyTenantQueryFilters(ModelBuilder builder)
    {
        var tenantEntityTypes = builder.Model
            .GetEntityTypes()
            .Where(entityType => typeof(ITenant).IsAssignableFrom(entityType.ClrType))
            .ToList();

        foreach (var entityType in tenantEntityTypes)
        {
            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(ApplyTenantQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, new object[] { builder });
        }
    }

    private void ApplyTenantQueryFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenant
    {
        builder.Entity<TEntity>()
            .HasQueryFilter(entity =>
                _tenantContextAccessor.CurrentTenantId == null
                || _tenantContextAccessor.ShouldBypassFilters
                || entity.TenantId == _tenantContextAccessor.CurrentTenantId);
    }
}

public partial class ApplicationDbContext
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void AddDomainEvent(IDomainEvent evt)
        => _domainEvents.Add(evt);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}

