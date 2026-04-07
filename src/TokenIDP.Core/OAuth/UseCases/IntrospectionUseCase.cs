using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class IntrospectionUseCase
{
    private readonly IAppLogger<IntrospectionUseCase> _logger;
    private readonly ITokenRepository _tokenStore;
    private readonly TokenSecretGenerator _tokenSecretGenerator;
    private readonly ICurrentUserService _currentUserService;
    public IntrospectionUseCase(IAppLogger<IntrospectionUseCase> logger,
        ITokenRepository tokenStore,
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

        if (!IsCallerAuthorized(referenceToken))
        {
            _logger.LogWarning(
                "Introspection denied for caller ClientId {CallerClientId}, TenantId {CallerTenantId} on token ClientId {TokenClientId}, TenantId {TokenTenantId}",
                _currentUserService.ClientId,
                _currentUserService.TenantId,
                referenceToken.ClientId,
                referenceToken.TenantId);

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


