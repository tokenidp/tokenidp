using IDP.Core.UseCases;

namespace IDP.Core.GrantHandlers;

internal sealed class ClientCredentialGrantHandler : ITokenGrantHandler
{
    private const string GrantTypeValue = "client_credentials";

    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TokenIssuerUseCase _tokenService;
    private readonly IAppLogger<ClientCredentialGrantHandler> _logger;

    public ClientCredentialGrantHandler(
        TokenContextUseCase tokenContextUseCase,
        TokenIssuerUseCase tokenService,
        IAppLogger<ClientCredentialGrantHandler> logger)
    {
        _tokenContextUseCase = tokenContextUseCase;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        var normalized = NormalizeRequest(request);

        var tokenContext = await _tokenContextUseCase.BuildClientCredentialTokenContextAsync(
            normalized.ClientId,
            normalized.Scope);

        ValidateClientSecret(normalized.ClientSecret, tokenContext.ActiveSecretHashes, normalized.ClientId);

        var token = await _tokenService.IssueTokenAsync(tokenContext);

        _logger.LogInfo("Client credentials token issued. ClientId={ClientId}, Scopes={Scopes}",
            normalized.ClientId,
            string.Join(' ', tokenContext.Scopes));

        return token;
    }

    private static NormalizedRequest NormalizeRequest(TokenRequest request)
    {
        if (request is null)
            throw new TokenRequestValidationException("invalid_request", "Request is missing.");

        if (!string.Equals(request.GrantType, GrantTypeValue, StringComparison.Ordinal))
            throw new TokenRequestValidationException("unsupported_grant_type", "Invalid grant_type.");

        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new TokenRequestValidationException("invalid_request", "client_id is required.");

        return new NormalizedRequest(
            request.ClientId.Trim(),
            request.Scope,
            request.IpAddress,
            request.ClientSecret
        );
    }

    private void ValidateClientSecret(string? providedSecret, IReadOnlySet<string> storedSecrets, string clientId)
    {
        if (!ClientSecretValidator.Matches(providedSecret, storedSecrets))
        {
            _logger.LogWarning("Client {ClientId} rejected: {Reason}.", clientId, "secret mismatch");

            throw new TokenRequestValidationException("invalid_client", "Client secret is invalid.");
        }
    }
    private sealed record NormalizedRequest(
        string ClientId,
        string? Scope,
        string? IpAddress,
        string? ClientSecret);
}