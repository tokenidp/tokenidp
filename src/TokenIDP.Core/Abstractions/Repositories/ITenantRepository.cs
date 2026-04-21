using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Tenants;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface ITenantRepository
{
    Task<bool> CheckTwoFactorEnabled(int tenantId);

    Task<TenantUISetting?> GetTenantUISettings(int tenantId);

    Task<TenantExternalProvider?> ResolveExternalProvider(
       int tenantId,
       ExternalProviderTypes providerType,
       CancellationToken ct = default);
    Task<Tenant?> GetTenantAggregateAsync(int tenantId, CancellationToken ct);
    Task<TenantDetail?> GetTenantDetailAsync(int tenantId, CancellationToken ct);
    Task<Tenant?> GetTenantWithProvidersAsync(int tenantId, CancellationToken ct);
    Task<TenantResolutionResult?> ResolveTenantAsync(string tenantKey, CancellationToken ct);
    Task<PaginatedList<TenantSearchResult>> SearchTenantsAsync(int? scopedTenantId, SearchData request, CancellationToken ct);
    Task<bool> TenantNameExistsAsync(string tenantName, int? excludeTenantId, CancellationToken ct);
    Task<bool> TenantKeyExistsAsync(string tenantKey, CancellationToken ct);
    Task<IReadOnlySet<int>> GetTenantExternalProviderIdsAsync(int tenantId, CancellationToken ct);
    Task<int> AddAsync(Tenant tenant, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<TenantAuthSetting?> GetTenantAuthSettingAsync(int tenantId, CancellationToken ct);
    Task<TenantSummary?> GetSummaryAsync(int tenantId, CancellationToken ct);
}

