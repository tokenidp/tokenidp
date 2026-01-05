namespace IDP.Core.OAuth;

internal sealed class PreAuthorizationService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;

    public PreAuthorizationService(ApplicationDbContext applicationDbContext,
        ICache cache)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
    }

    internal async Task<UserPreAuthorization> GetPreAuthorization(string correlationId, int userId)
    {
        var cacheKey = CacheKeys.PRE_AUTHORIZATION.FormatCacheKey(correlationId, userId);

        var preAuthorization = await _cache.GetAsync<UserPreAuthorization>(cacheKey);

        if (preAuthorization == null)
        {
            preAuthorization = await _applicationDbContext.PreAuthorizations
                   .Where(t => t.CorrelationId == correlationId && t.UserId == userId
                                && t.Expiry > DateTime.UtcNow && !t.Is2FAVerified)
                   .OrderByDescending(t => t.Id)
                   .FirstOrDefaultAsync();
        }

        return preAuthorization;
    }

    internal async Task AddPreAuthorization(UserPreAuthorization preAuthorization)
    {
        _applicationDbContext.PreAuthorizations.Add(preAuthorization);
        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));
    }

    internal async Task UpdatePreAuthorization(UserPreAuthorization preAuthorization)
    {
        _applicationDbContext.PreAuthorizations.Update(preAuthorization);
        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));
    }
}
