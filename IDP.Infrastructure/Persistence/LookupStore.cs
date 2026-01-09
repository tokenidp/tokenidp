namespace IDP.Infrastructure.Persistence;

internal sealed class LookupStore : ILookupStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<LookupStore> _logger;

    public LookupStore(IApplicationDbContext dbContext,
        IAppLogger<LookupStore> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<LookupValue>> GeTenantLookupsByType(int tenantId, string type)
    {
        var cacheKey = CacheKeys.LOOKUP.FormatCacheKey(type, tenantId);

        var lookupValues = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            return await (from lt in _dbContext.LookupTypes
                          join lv in _dbContext.LookupValues on lt.Id equals lv.LookupTypeId
                          where lt.LookupTypeName == type && lt.TenantId == tenantId
                          select lv).ToListAsync();

        }, new TimeSpan(0, 45, 0));

        return lookupValues;
    }
}
