using IDP.Domain.AggregateRoots.Authorization;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Infrastructure.Persistence;

internal sealed class AuthorizationCodeStore : IAuthorizationCodeStore
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<AuthorizationCodeStore> _logger;

    public AuthorizationCodeStore(IApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<AuthorizationCodeStore> logger)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> Create(AuthorizationCode authorizationCode)
    {
        _applicationDbContext.AuthorizationCodes.Add(authorizationCode);

        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.AUTHORIZATION
            .FormatCacheKey(authorizationCode.Code);

        await _cache.SetAsync(cacheKey, authorizationCode, new TimeSpan(0, 5, 0));

        return authorizationCode.Id;
    }

    public async Task<AuthorizationCode?> GetByCode(string code)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(code);

        var authorizationCode = await _cache.GetAsync<AuthorizationCode>(cacheKey);

        if (authorizationCode == null)
        {
            authorizationCode = await _applicationDbContext.AuthorizationCodes
                .FirstOrDefaultAsync(x => x.Code == code && x.Expiry > DateTime.UtcNow && !x.IsUsed);
        }

        return authorizationCode;
    }



    public async Task<int> Update(AuthorizationCode authorizationCode)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(authorizationCode.Code);

        authorizationCode.UpdateIsUsed(true);

        var id = await _applicationDbContext.SaveChangesAsync();

        await _cache.RemoveAsync(cacheKey);

        return authorizationCode.Id;
    }
}
