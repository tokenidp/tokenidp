namespace IDP.Service.Infrastructure;

public class LookupRepo
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;

    public LookupRepo(ApplicationDbContext applicationDbContext, ICache cache)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
    }

    public async Task<IEnumerable<LookupValue>> GeTenantLookupsByType(int tenantId, string type)
    {
        var cacheKey = CacheKeys.LOOKUP.FormatCacheKey(tenantId);

        var lookupValues = _cache.GetValue<IEnumerable<LookupValue>>(cacheKey);

        if (!lookupValues.IsSafe())
        {
            lookupValues = await (from lt in _applicationDbContext.LookupTypes
                                  join lv in _applicationDbContext.LookupValues on lt.Id equals lv.LookupTypeId
                                  where lt.LookupTypeName == type && lt.TenantId == tenantId
                                  select lv).ToListAsync();

            _cache.Add(cacheKey, lookupValues, new TimeSpan(0, 45, 0));
        }

        return lookupValues;
    }
}
