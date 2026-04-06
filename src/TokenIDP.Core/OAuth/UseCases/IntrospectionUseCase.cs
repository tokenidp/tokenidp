using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class IntrospectionUseCase
{
    private readonly IAppLogger<IntrospectionUseCase> _logger;
    private readonly ITokenStore _tokenStore;
    private readonly TokenSecretGenerator _tokenSecretGenerator;
    private readonly ICurrentUserService _currentUserService;
    public IntrospectionUseCase(IAppLogger<IntrospectionUseCase> logger,
        ITokenStore tokenStore,
        TokenSecretGenerator tokenSecretGenerator,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _tokenStore = tokenStore;
        _tokenSecretGenerator = tokenSecretGenerator;
        _currentUserService = currentUserService;
    }

    public async Task<IntrospectionResponse> ValidateReferenceToken(string token)
    {
        _logger.LogDebug("Validating reference token: {TokenId}", token);

        var tokenHash = _tokenSecretGenerator.HashToken(token!);

        var referenceToken = await _tokenStore.GetToken(tokenHash);

        if (referenceToken == null)
        {
            _logger.LogWarning("Reference token not found, expired or revoked: {TokenId}",
                $"{token.SubstringSafe(0, 5)}...");

            return IntrospectionResponse.Inactive();
        }

        _logger.LogDebug("Valid token found for client {client}", referenceToken.ClientId);

        var roles = referenceToken.Roles ?? string.Empty;

        return IntrospectionResponse.ActiveResponse(
            referenceToken.UserId?.ToString() ?? string.Empty,
            referenceToken.ClientId,
            referenceToken.TenantId.ToString(),
            referenceToken.Scope,
            roles.Split(","),
            referenceToken.ExpiresAt,
            referenceToken.IssuedAt,
            _currentUserService.BaseUrl
            );
    }
}

