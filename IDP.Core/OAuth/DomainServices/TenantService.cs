namespace IDP.Core.OAuth.DomainServices;

internal class TenantService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;

    public TenantService(ApplicationDbContext dbContext, ICache cache)
    {
        _dbContext = dbContext;
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
