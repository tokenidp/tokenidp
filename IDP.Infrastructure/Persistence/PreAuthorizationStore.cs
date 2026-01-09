using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Infrastructure.Persistence;

internal class PreAuthorizationStore : IPreAuthorizationStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<RoleStore> _logger;

    public PreAuthorizationStore(IApplicationDbContext dbContext,
        IAppLogger<RoleStore> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<int> Create(PreAuthorization preAuthorization)
    {
        _dbContext.PreAuthorizations.Add(preAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));

        return id;
    }

    public async Task<PreAuthorization?> GetPreAuthorization(string correlationId, int userId)
    {
        var cacheKey = CacheKeys.PRE_AUTHORIZATION.FormatCacheKey(correlationId, userId);

        var preAuthorization = await _cache.GetAsync<PreAuthorization>(cacheKey);

        if (preAuthorization == null)
        {
            preAuthorization = await _dbContext.PreAuthorizations
                   .Where(t => t.CorrelationId == correlationId && t.UserId == userId
                                && t.Expiry > DateTime.UtcNow && !t.Is2FAVerified)
                   .OrderByDescending(t => t.Id)
                   .FirstOrDefaultAsync();
        }

        return preAuthorization;
    }

    public async Task<int> Update(PreAuthorization preAuthorization)
    {
        _dbContext.PreAuthorizations.Update(preAuthorization);

        var id = await _dbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));

        return preAuthorization.Id;
    }
}
