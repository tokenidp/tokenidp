using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.OAuth.UseCases;

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

        var token = await _tokenStore.GetToken(tokenHash);

        if (token == null)
        {
            _logger.LogError("Token not found.");

            throw new NotFoundException("Token not found.");
        }

        _logger.LogDebug("Token found for {UserId} for token revocation", _currentUserService.UserId);

        token.Revoke(request.ReasonRevoked, request.IpAddress, _currentUserService.UserId);

        _logger.LogDebug("Marked token as revoked at {RevocationTime}", DateTime.UtcNow);

        await _tokenStore.RevokeToken(token);

        _logger.LogInfo("Successfully revoked token for user {UserId}", _currentUserService.UserId);
    }
}
