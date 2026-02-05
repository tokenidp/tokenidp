using IDP.Domain.AggregateRoots.Outbox;
using IDP.Domain.ReadModels;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IDP.Foundation.Abstractions;

public interface IApplicationDbContext
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<PreAuthorization> PreAuthorizations { get; }
    DbSet<AuthorizationCode> AuthorizationCodes { get; }
    DbSet<Client> Clients { get; }
    DbSet<ClientScope> ClientScopes { get; }
    DbSet<ClientAudience> ClientAudiences { get; }
    DbSet<ClientSecret> ClientSecrets { get; }
    DbSet<ClientGrantType> ClientGrantTypes { get; }
    DbSet<Token> Tokens { get; }
    DbSet<ReferenceToken> ReferenceTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OutboxEvent> OutboxEvents { get; }
    DbSet<OutboxEventConsumer> OutboxEventConsumers { get; }
    DbSet<LookupType> LookupTypes { get; }
    DbSet<LookupValue> LookupValues { get; }
    DbSet<Configuration> Configurations { get; }
    DbSet<UserRolePermission> UserRolePermissions { get; }
    DbSet<UserSearch> UsersSearch { get; }
    DbSet<RoleSearch> RolesSearch { get; }
    DbSet<TenantSearch> TenantsSearch { get; }
    DbSet<ConfigurationSearch> ConfigurationsSearch { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<UserAddress> UserAddresses { get; }
    DbSet<UserContact> UserContacts { get; }
    DbSet<TokenSearch> TokenSearch { get; }
    DbSet<TokenReadModel> TokenReadModel { get; }
    DbSet<Activity> Activities { get; }
    DbSet<DashboardMetric> DashboardMetrics { get; }
    DbSet<DashboardMetricsCheckpoint> DashboardMetricsCheckpoints { get; }
    DbSet<DashboardMetricRanking> DashboardMetricRankings { get; }

    DatabaseFacade Database { get; }

    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
}
