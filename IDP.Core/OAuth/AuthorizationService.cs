namespace IDP.Core.OAuth;

internal class AuthorizationService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<AuthorizationService> _logger;

    public AuthorizationService(ApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<AuthorizationService> logger)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UserAuthorizationCode> ValidateAuthorizationCode(string code)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(code);

        var authorizationCode = await _cache.GetAsync<UserAuthorizationCode>(cacheKey);

        if (authorizationCode == null)
        {
            authorizationCode = await _applicationDbContext.AuthorizationCodes
                .FirstOrDefaultAsync(x => x.Code == code && x.Expiry > DateTime.UtcNow && !x.IsUsed);
        }

        if (authorizationCode == null)
        {
            _logger.LogWarning("Authorization code {code} not found or expired.", code);

            throw new UnauthorizedAccessException("Authorization Code not found.");
        }

        _logger.LogInfo("Authorization code found for UserId: {UserId}", authorizationCode.UserId);

        authorizationCode.UpdateIsUsed(true);

        await _applicationDbContext.SaveChangesAsync();
        return authorizationCode;
    }

    public async Task SaveAuthorization(UserAuthorizationCode authorizationCode)
    {
        _applicationDbContext.AuthorizationCodes.Add(authorizationCode);

        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.AUTHORIZATION
            .FormatCacheKey(authorizationCode.Code);

        await _cache.SetAsync(cacheKey, authorizationCode, new TimeSpan(0, 5, 0));
    }
}
