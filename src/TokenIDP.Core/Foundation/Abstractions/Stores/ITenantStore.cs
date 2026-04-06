using TokenIDP.Domain.AggregateRoots.Tenants;

namespace TokenIDP.Core.Foundation.Abstractions.Stores;

public interface ITenantStore
{
    Task<bool> CheckTwoFactorEnabled(int tenantId);

    Task<TenantUISetting?> GetTenantUISettings(int tenantId);

    Task<TenantExternalProvider?> ResolveExternalProvider(
       int tenantId,
       ExternalProviderTypes providerType,
       CancellationToken ct = default);
}

