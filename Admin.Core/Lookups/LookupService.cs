namespace Admin.Core.Lookups;

internal class LookupService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;

    public LookupService(ApplicationDbContext applicationDbContext, ICache cache)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
    }

    public async Task<IEnumerable<LookupValue>> GeTenantLookupsByType(int tenantId, string type)
    {
        var cacheKey = CacheKeys.LOOKUP.FormatCacheKey(type, tenantId);

        var lookupValues = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            return await (from lt in _applicationDbContext.LookupTypes
                          join lv in _applicationDbContext.LookupValues on lt.Id equals lv.LookupTypeId
                          where lt.LookupTypeName == type && lt.TenantId == tenantId
                          select lv).ToListAsync();

        }, new TimeSpan(0, 45, 0));

        return lookupValues;
    }
}
