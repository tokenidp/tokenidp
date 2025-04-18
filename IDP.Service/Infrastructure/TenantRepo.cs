namespace IDP.Service.Infrastructure;

public class TenantRepo
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;

    public TenantRepo(ApplicationDbContext applicationDbContext, ICache cache)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
    }

    public async Task<bool> CheckTwoFactorEnabled(int tenantId)
    {
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("TwoFactor", tenantId);

        var value = _cache.GetValue<string>(cacheKey);

        if (string.IsNullOrEmpty(value))
        {
            var response = await _applicationDbContext.Tenants.Where(t => t.Id == tenantId)
           .Select(s => s.TwoFactorEnabled)
           .FirstOrDefaultAsync();

            value = response ? "Yes" : "No";

            _cache.Add(cacheKey, value, new TimeSpan(0, 15, 0));
        }

        return value == "Yes";
    }
}
