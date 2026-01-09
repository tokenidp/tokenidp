using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Core.Services;

internal sealed class AuthorizationCodeService
{
    private readonly IAppLogger<AuthorizationCodeService> _logger;
    private readonly IAuthorizationCodeStore _authorizationCodeStore;

    public AuthorizationCodeService(IAppLogger<AuthorizationCodeService> logger,
        IAuthorizationCodeStore authorizationCodeStore)
    {
        _logger = logger;
        _authorizationCodeStore = authorizationCodeStore;
    }

    internal async Task<AuthResponse> GenerateAuthorizationCode(AuthRequest request, int userId)
    {
        var code = Guid.NewGuid().ToString();
        _logger.LogDebug("Generated authorization code: {Code}", code);

        AuthorizationCode authorizationCode = new(
            code,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            request.ClientId,
            userId,
            DateTime.UtcNow.AddMinutes(5),
            request.RedirectUri,
            request.Scopes);

        var id = await _authorizationCodeStore.Create(authorizationCode);

        _logger.LogInfo("Saved authorization code {Id} for user {UserId} - Client: {ClientId}.",
            id, userId, request.ClientId);

        return AuthResponse.Success(code);
    }

    internal async Task<AuthorizationCode> ValidateAuthorizationCode(string code)
    {
        var authorizationCode = await _authorizationCodeStore.GetByCode(code);

        if (authorizationCode == null || authorizationCode.Expiry <= DateTime.UtcNow
            || authorizationCode.IsUsed || authorizationCode.Code != code)
        {
            _logger.LogWarning("Authorization code {code} not found or expired.", code);

            throw new UnauthorizedAccessException("Authorization code {code} not found or expired.");
        }

        _logger.LogInfo("Authorization code found for UserId: {UserId}", authorizationCode.UserId);

        var id = _authorizationCodeStore.Update(authorizationCode);

        return authorizationCode;
    }
}
