using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.OAuth.GrantHandlers;

internal sealed class RefreshTokenGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<RefreshTokenGrantHandler> _logger;
    private readonly ITokenStore _tokenStore;
    private readonly TokenIssuerUseCase _tokenService;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TokenSecretGenerator _tokenSecretGenerator;

    public RefreshTokenGrantHandler(IAppLogger<RefreshTokenGrantHandler> logger,
        ITokenStore tokenStore,
        TokenContextUseCase tokenContextUseCase,
        TokenIssuerUseCase tokenService,
        TokenSecretGenerator tokenSecretGenerator)
    {
        _logger = logger;
        _tokenService = tokenService;
        _tokenStore = tokenStore;
        _tokenContextUseCase = tokenContextUseCase;
        _tokenSecretGenerator = tokenSecretGenerator;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new TokenRequestValidationException("invalid_request", "Missing refresh_token.");
        }

        var tokenHash = _tokenSecretGenerator.HashToken(request.RefreshToken!);

        var existingToken = await _tokenStore.GetRefreshToken(tokenHash);

        if (existingToken?.RefreshToken == null)
        {
            _logger.LogWarning("Refresh token not found.");

            throw new TokenRequestValidationException("invalid_grant", "Invalid refresh token.");
        }

        if (!string.Equals(existingToken.ClientId, request.ClientId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Refresh token client mismatch. Requested client {RequestedClientId}, token client {TokenClientId}",
                request.ClientId,
                existingToken.ClientId);

            throw new TokenRequestValidationException("invalid_grant", "Invalid refresh token.");
        }

        if (existingToken.RefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token used for client {ClientId}", request.ClientId);

            throw new TokenRequestValidationException("invalid_grant", "Invalid refresh token.");
        }

        if (existingToken.RefreshToken.IsConsumed)
        {
            _logger.LogWarning("Refresh token reuse detected for client {ClientId}", request.ClientId);

            existingToken.DetectRefreshTokenReuse();
            existingToken.Revoke(
                RevokeReason.RefreshReuse.ToString(),
                request.IpAddress ?? string.Empty,
                existingToken.UserId ?? 0);

            await _tokenStore.RevokeToken(existingToken);

            throw new TokenRequestValidationException("invalid_grant", "Invalid refresh token.");
        }

        if (!existingToken.UserId.HasValue || existingToken.UserId.Value <= 0)
        {
            _logger.LogWarning("Refresh token is not associated with a user for client {ClientId}", request.ClientId);

            throw new TokenRequestValidationException("invalid_grant", "Invalid refresh token.");
        }

        var tokenInfo = await _tokenContextUseCase
            .BuildTokenContextAsync(
                request.ClientId,
                existingToken.UserId.Value,
                GrantTypes.refresh_token,
                request.Scope.IsSafe() ? request.Scope : existingToken.Scope);

        var newRefreshTokenId = Guid.NewGuid();

        existingToken.RotateRefreshToken(newRefreshTokenId);

        _logger.LogInfo("Generating refresh token for client {ClientId} from {IPAddress}",
            request.ClientId, request.IpAddress ?? string.Empty);

        var token = await _tokenService.IssueTokenAsync(
            tokenInfo,
            existingToken.RefreshToken.Id,
            newRefreshTokenId);

        _logger.LogInfo("Successfully saved new refresh token for client {clientId}", existingToken.ClientId);

        return token;
    }
}
