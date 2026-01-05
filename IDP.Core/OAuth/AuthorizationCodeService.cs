using IDP.Core.Model;

namespace IDP.Core.OAuth;

internal sealed class AuthorizationCodeService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<AuthorizationCodeService> _logger;

    public AuthorizationCodeService(ApplicationDbContext applicationDbContext,
        ICache cache,
        IAppLogger<AuthorizationCodeService> logger)
    {
        _applicationDbContext = applicationDbContext;
        _cache = cache;
        _logger = logger;
    }

    internal async Task<AuthResponse> GenerateAuthorizationCode(AuthRequest request, int userId)
    {
        var code = Guid.NewGuid().ToString();
        _logger.LogDebug("Generated authorization code: {Code}", code);

        UserAuthorizationCode authorizationCode = new(
            code,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            request.ClientId,
            userId,
            DateTime.UtcNow.AddMinutes(5),
            request.RedirectUri,
            request.Scopes);

        await SaveAuthorization(authorizationCode);

        _logger.LogInfo("Saved authorization code for user {UserId} (Client: {ClientId})",
            userId, request.ClientId);

        return AuthResponse.Success(code);
    }

    internal async Task<UserAuthorizationCode> ValidateAuthorizationCode(string code)
    {
        var cacheKey = CacheKeys.AUTHORIZATION.FormatCacheKey(code);

        var authorizationCode = await _cache.GetAsync<UserAuthorizationCode>(cacheKey);

        if (authorizationCode == null)
        {
            authorizationCode = await _applicationDbContext.AuthorizationCodes
                .FirstOrDefaultAsync(x => x.Code == code && x.Expiry > DateTime.UtcNow && !x.IsUsed);
        }

        if (authorizationCode == null || authorizationCode.Expiry <= DateTime.UtcNow
            || authorizationCode.IsUsed || authorizationCode.Code != code)
        {
            _logger.LogWarning("Authorization code {code} not found or expired.", code);

            throw new UnauthorizedAccessException("Authorization code {code} not found or expired.");
        }

        _logger.LogInfo("Authorization code found for UserId: {UserId}", authorizationCode.UserId);

        authorizationCode.UpdateIsUsed(true);

        await _applicationDbContext.SaveChangesAsync();

        await _cache.RemoveAsync(cacheKey);

        return authorizationCode;
    }

    private async Task SaveAuthorization(UserAuthorizationCode authorizationCode)
    {
        _applicationDbContext.AuthorizationCodes.Add(authorizationCode);

        await _applicationDbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.AUTHORIZATION
            .FormatCacheKey(authorizationCode.Code);

        await _cache.SetAsync(cacheKey, authorizationCode, new TimeSpan(0, 5, 0));
    }
}
