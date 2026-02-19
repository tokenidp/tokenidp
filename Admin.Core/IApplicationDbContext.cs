using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Configurations;
using IDP.Domain.AggregateRoots.Emails;
using IDP.Domain.AggregateRoots.Outbox;
using IDP.Domain.AggregateRoots.Permissions;
using IDP.Domain.AggregateRoots.Tokens;
using IDP.Domain.ReadModels;

namespace Admin.Core;

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
    DbSet<Configuration> Configurations { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<UserCodeSequence> UserCodeSequences { get; }
    DbSet<UserAddress> UserAddresses { get; }
    DbSet<UserContact> UserContacts { get; }

    // ------------Emails-------------------

    DbSet<EmailMessage> EmailMessages { get; }
    DbSet<EmailDeliveryAttempt> EmailDeliveryAttempts { get; }

    // ------------Read Models-------------------

    DbSet<TokenSearch> TokenSearch { get; }
    DbSet<TokenReadModel> TokenReadModel { get; }
    DbSet<UserRolePermission> UserRolePermissions { get; }
    DbSet<ConfigurationSearch> ConfigurationsSearch { get; }
    DbSet<UserSearch> UsersSearch { get; }
    DbSet<RoleSearch> RolesSearch { get; }
    DbSet<TenantSearch> TenantsSearch { get; }
    DbSet<Activity> Activities { get; }
    DbSet<DashboardMetric> DashboardMetrics { get; }
    DbSet<DashboardMetricsCheckpoint> DashboardMetricsCheckpoints { get; }
    DbSet<DashboardMetricRanking> DashboardMetricRankings { get; }

    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
}
