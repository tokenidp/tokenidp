namespace Admin.Core.Lookups;

internal class LookupService
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<LookupService> _logger;

    public LookupService(IApplicationDbContext applicationDbContext, ICache cache, IAppLogger<LookupService> logger)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<LookupValue>> GeTenantLookupsByType(int tenantId, string type)
    {
        _logger.LogDebug("Fetching lookup values for tenant {TenantId} and type {Type}", tenantId, type);

        var cacheKey = CacheKeys.LOOKUP.FormatCacheKey(type, tenantId);

        var lookupValues = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            return await (from lt in _applicationDbContext.LookupTypes
                          join lv in _applicationDbContext.LookupValues on lt.Id equals lv.LookupTypeId
                          where lt.LookupTypeName == type && lt.TenantId == tenantId
                          select lv).ToListAsync();

        }, new TimeSpan(0, 45, 0));

        _logger.LogDebug("Lookup values fetched for tenant {TenantId} and type {Type}", tenantId, type);

        return lookupValues;
    }
}
