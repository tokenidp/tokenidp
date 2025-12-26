using IDP.Core.Common.Extensions;
using IDP.Core.Common.Interfaces;
using IDP.Core.Domain.AggregateRoots.Users;

namespace IDP.Core.Application;

internal class PreAuthorizationRepo
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;

    public PreAuthorizationRepo(ApplicationDbContext applicationDbContext,
        ICache cache)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
    }

    public async Task<PreAuthorization> GetPreAuthorization(string correlationId, int userId)
    {
        var cacheKey = CacheKeys.PRE_AUTHORIZATION.FormatCacheKey(correlationId, userId);

        var preAuthorization = await _cache.GetAsync<PreAuthorization>(cacheKey);

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

    public async Task AddPreAuthorization(PreAuthorization preAuthorization)
    {
        _applicationDbContext.PreAuthorizations.Add(preAuthorization);
        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));
    }

    public async Task UpdatePreAuthorization(PreAuthorization preAuthorization)
    {
        _applicationDbContext.PreAuthorizations.Update(preAuthorization);
        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.PRE_AUTHORIZATION
            .FormatCacheKey(preAuthorization.CorrelationId, preAuthorization.UserId);

        await _cache.SetAsync(cacheKey, preAuthorization, new TimeSpan(0, 5, 0));
    }
}
