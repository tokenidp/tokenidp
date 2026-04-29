using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class RevokeTokenUseCase
{
    private readonly ITokenRepository _tokenStore;
    private readonly IAppLogger<RevokeTokenUseCase> _logger;
    private ICurrentUserService _currentUserService;
    private readonly TokenSecretGenerator _tokenSecretGenerator;

    public RevokeTokenUseCase(IAppLogger<RevokeTokenUseCase> logger,
        ITokenRepository tokenStore,
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
            _logger.LogWarning("Token not found or already inactive during revocation.");
            return;
        }

        if (!IsCallerAuthorized(token))
        {
            _logger.LogWarning(
                "Revocation denied for caller ClientId {CallerClientId}, TenantId {CallerTenantId} on token ClientId {TokenClientId}, TenantId {TokenTenantId}",
                _currentUserService.ClientId,
                _currentUserService.TenantId,
                token.ClientId,
                token.TenantId);

            return;
        }

        _logger.LogDebug("Token found for {UserId} for token revocation", _currentUserService.UserId);

        token.Revoke(request.ReasonRevoked, request.IpAddress, _currentUserService.UserId);

        _logger.LogDebug("Marked token as revoked at {RevocationTime}", DateTime.UtcNow);

        await _tokenStore.RevokeToken(token);

        _logger.LogInfo("Successfully revoked token for user {UserId}", _currentUserService.UserId);
    }

    private bool IsCallerAuthorized(Token token)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.ClientId))
        {
            return false;
        }

        if (!string.Equals(_currentUserService.ClientId, token.ClientId, StringComparison.Ordinal) ||
            _currentUserService.TenantId != token.TenantId)
        {
            return false;
        }

        if (_currentUserService.UserId <= 0)
        {
            return true;
        }

        return token.UserId.HasValue &&
               token.UserId.Value == _currentUserService.UserId;
    }
}

