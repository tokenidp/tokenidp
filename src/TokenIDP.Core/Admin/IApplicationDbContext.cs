using TokenIDP.Domain.AggregateRoots;
using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Configurations;
using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Domain.AggregateRoots.Tokens;
using TokenIDP.Domain.ReadModels;

namespace TokenIDP.Core.Admin;

public interface IApplicationDbContext
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<TenantAuthSetting> TenantAuthSettings { get; }
    DbSet<TenantExternalProvider> TenantExternalProviders { get; }
    DbSet<TenantUISetting> TenantUISettings { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<PreAuthorization> PreAuthorizations { get; }
    DbSet<AuthorizationCode> AuthorizationCodes { get; }
    DbSet<DeviceAuthorization> DeviceAuthorizations { get; }
    DbSet<Client> Clients { get; }
    DbSet<ClientScope> ClientScopes { get; }
    DbSet<ClientApiResource> ClientApiResources { get; }
    DbSet<ApiResource> ApiResources { get; }
    DbSet<ApiScope> ApiScopes { get; }
    DbSet<ClientSecret> ClientSecrets { get; }
    DbSet<ClientGrantType> ClientGrantTypes { get; }
    DbSet<ClientAuthPolicy> ClientAuthPolicies { get; }
    DbSet<ClientExternalProvider> ClientExternalProviders { get; }
    DbSet<Token> Tokens { get; }
    DbSet<ReferenceToken> ReferenceTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OutboxEvent> OutboxEvents { get; }
    DbSet<OutboxEventConsumer> OutboxEventConsumers { get; }
    DbSet<Configuration> Configurations { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<UserExternalLogin> UserExternalLogins { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; }
    DbSet<CodeSequence> CodeSequences { get; }
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

