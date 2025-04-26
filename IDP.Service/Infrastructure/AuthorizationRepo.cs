namespace IDP.Service.Infrastructure;

public class AuthorizationRepo
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<AuthorizationRepo> _logger;

    public AuthorizationRepo(ApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<AuthorizationRepo> logger)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AuthorizationCode> ValidateAuthorizationCode(string code, int userId)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(code, userId);

        var authorizationCode = await _cache.GetAsync<AuthorizationCode>(cacheKey);

        if (authorizationCode == null)
        {
            authorizationCode = await _applicationDbContext.AuthorizationCodes
                .FirstOrDefaultAsync(x => x.Code == code && x.Expiry > DateTime.UtcNow && !x.IsUsed);
        }

        if (authorizationCode == null)
        {
            _logger.LogWarning("Authorization code not found or expired for UserId: {UserId}", userId);
            throw new UnauthorizedAccessException("Authorization Code not found.");
        }

        _logger.LogInfo("Authorization code found for UserId: {UserId}", authorizationCode.UserId);

        authorizationCode.UpdateIsUsed(true, userId);

        await _applicationDbContext.SaveChangesAsync();
        return authorizationCode;
    }

    public async Task SaveAuthorization(AuthorizationCode authorizationCode)
    {
        _applicationDbContext.AuthorizationCodes.Add(authorizationCode);
        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.AUTHORIZATION
            .FormatCacheKey(authorizationCode.Code, authorizationCode.UserId);

        await _cache.SetAsync(cacheKey, authorizationCode, new TimeSpan(0, 5, 0));
    }
}
