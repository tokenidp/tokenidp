using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class RevokeTokenUseCase
{
    private readonly ITokenStore _tokenStore;
    private readonly IAppLogger<RevokeTokenUseCase> _logger;
    private ICurrentUserService _currentUserService;
    private readonly TokenSecretGenerator _tokenSecretGenerator;

    public RevokeTokenUseCase(IAppLogger<RevokeTokenUseCase> logger,
        ITokenStore tokenStore,
        ICurrentUserService currentUserService,
        TokenSecretGenerator tokenSecretGenerator)
    {
        _logger = logger;
        _tokenStore = tokenStore;
        _currentUserService = currentUserService;
        _tokenSecretGenerator = tokenSecretGenerator;
    }

    internal async Task RevokeToken(RevokeTokenRequest request)
    {
        var tokenHash = _tokenSecretGenerator.HashToken(request.Token!);

        var token = await _tokenStore.GetReferenceToken(tokenHash);

        if (token == null)
        {
            _logger.LogWarning("Reference token not found.");

            throw new NotFoundException("Refresh token not found.");
        }

        _logger.LogDebug("Refresh token found for {UserId} for token revocation", _currentUserService.UserId);

        token.Revoke(request.ReasonRevoked, _currentUserService.UserId);

        _logger.LogDebug("Marked token as revoked at {RevocationTime}", DateTime.UtcNow);

        await _tokenStore.RevokeToken(token);

        _logger.LogInfo("Successfully revoked refresh token for user {UserId}", _currentUserService.UserId);
    }
}
