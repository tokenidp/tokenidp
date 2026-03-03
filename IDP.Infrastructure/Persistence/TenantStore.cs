using Admin.Core.Tenants;
using IDP.Domain.AggregateRoots.Tenants;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Infrastructure.Persistence;

internal sealed class TenantStore : ITenantStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<TenantStore> _logger;

    public TenantStore(IApplicationDbContext dbContext,
        IAppLogger<TenantStore> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> CheckTwoFactorEnabled(int tenantId)
    {
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("TwoFactor", tenantId);

        var hasTwoFactorEnabled = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _dbContext.Tenants.Where(t => t.Id == tenantId)
            .Select(s => s.TenantAuthSetting.TwoFactor.IsEnabled)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        return hasTwoFactorEnabled;
    }

    public async Task<TenantUISetting?> GetTenantUISettings(int tenantId)
    {
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("UI", tenantId);

        var uiSetting = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _dbContext.TenantUISettings
            .Where(t => t.Id == tenantId)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        return uiSetting;
    }

    public async Task<TenantExternalProvider?> ResolveExternalProvider(
       int tenantId,
       ExternalProviderTypes providerType,
       CancellationToken ct = default)
    {
        var provider = await _dbContext.TenantExternalProviders
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId
                  && x.ProviderType == providerType
                  && x.Enabled,
                ct);

        if (provider == null)
            return null;

        return provider;
    }
}
