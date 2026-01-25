using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class IntrospectionUseCase
{
    private readonly IAppLogger<IntrospectionUseCase> _logger;
    private readonly ITokenStore _tokenStore;
    private readonly TokenSecretGenerator _tokenSecretGenerator;

    public IntrospectionUseCase(IAppLogger<IntrospectionUseCase> logger,
        ITokenStore tokenStore,
        TokenSecretGenerator tokenSecretGenerator)
    {
        _logger = logger;
        _tokenStore = tokenStore;
        _tokenSecretGenerator = tokenSecretGenerator;
    }

    public async Task<IntrospectionResponse> ValidateReferenceToken(string token)
    {
        _logger.LogDebug("Validating reference token: {TokenId}", token);

        var tokenHash = _tokenSecretGenerator.HashToken(token!);

        var referenceToken = await _tokenStore.GetReferenceToken(tokenHash);

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
            referenceToken.Scope,
            referenceToken.Roles.Split(","));
    }
}
