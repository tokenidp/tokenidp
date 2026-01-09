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
            .Select(s => s.TwoFactorEnabled)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        return hasTwoFactorEnabled;
    }
}
