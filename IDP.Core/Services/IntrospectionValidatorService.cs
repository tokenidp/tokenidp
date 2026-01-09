namespace IDP.Core.Services;

internal sealed class IntrospectionValidatorService
{
    private readonly IAppLogger<RevokeTokenService> _logger;
    private readonly ITokenStore _tokenStore;

    public IntrospectionValidatorService(IAppLogger<RevokeTokenService> logger,
        ITokenStore tokenStore)
    {
        _logger = logger;
        _tokenStore = tokenStore;
    }

    internal async Task<IntrospectionResponse> ValidateReferenceToken(string token)
    {
        _logger.LogDebug("Validating reference token: {TokenId}", token);

        var referenceToken = await _tokenStore.GetReferenceToken(token);

        if (referenceToken == null)
        {
            _logger.LogWarning("Reference token not found or revoked: {TokenId}",
                $"{token.SubstringSafe(0, 5)}...");

            return IntrospectionResponse.Create();
        }

        _logger.LogDebug("Valid token found for user {UserId}", referenceToken.UserId);

        return IntrospectionResponse.Create(
            referenceToken.UserId,
            referenceToken.TenantId,
            referenceToken.Scopes,
            referenceToken.Roles.Split(","));
    }
}
