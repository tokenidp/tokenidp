namespace IDP.Core.Services;

internal sealed class RevokeTokenService
{
    private readonly ITokenStore _tokenStore;
    private readonly IAppLogger<RevokeTokenService> _logger;

    public RevokeTokenService(IAppLogger<RevokeTokenService> logger,
        ITokenStore tokenStore)
    {
        _logger = logger;
        _tokenStore = tokenStore;
    }

    internal async Task RevokeToken(RevokeTokenRequest request)
    {
        var referenceToken = await _tokenStore.GetReferenceToken(request.Token);

        if (referenceToken == null)
        {
            _logger.LogWarning("Reference token not found.");

            throw new NotFoundException("Refresh token not found.");
        }

        _logger.LogDebug("Refresh token found for {UserId} for token revocation", request.UserId);

        referenceToken.RevokeToken(request.UserId);

        _logger.LogDebug("Marked token as revoked at {RevocationTime}", DateTime.UtcNow);

        await _tokenStore.RevokeToken(referenceToken);

        _logger.LogInfo("Successfully revoked refresh token for user {UserId}", referenceToken.Id);
    }
}
