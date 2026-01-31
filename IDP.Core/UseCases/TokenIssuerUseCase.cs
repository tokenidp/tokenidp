using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class TokenIssuerUseCase
{
    private readonly IAppLogger<TokenIssuerUseCase> _logger;
    private readonly ITokenStore _tokenStore;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly TokenSecretGenerator _tokenSecretGenerator;

    public TokenIssuerUseCase(JwtTokenGenerator tokenGenerator,
        IAppLogger<TokenIssuerUseCase> logger,
        ITokenStore tokenStore,
        ICurrentUserService currentUserService,
        TokenSecretGenerator tokenSecretGenerator)
    {
        _tokenGenerator = tokenGenerator;
        _logger = logger;
        _tokenStore = tokenStore;
        _currentUserService = currentUserService;
        _tokenSecretGenerator = tokenSecretGenerator;
    }

    internal async Task<TokenResponse> IssueTokenAsync(TokenContext context)
    {
        context.SetTokenDates();

        var token = Token.CreateToken(context);

        switch (context.TokenType)
        {
            case TokenTypes.JWT:
                {
                    var tokenResponse = CreateAccessToken(context, token.ExpiresAt, token.Id.ToString());

                    var refreshToken = await IssueRefreshToken(token, context);

                    tokenResponse.AddRefreshToken(refreshToken);

                    token.IssueJwt(context.ClientName, context.UserName);

                    await _tokenStore.CreateToken(token);

                    return tokenResponse;
                }
            case TokenTypes.ReferenceToken:
                {
                    var tokenResponse = CreateReferenceToken(context, token.ExpiresAt);

                    var hashToken = _tokenSecretGenerator.HashToken(tokenResponse.AccessToken);

                    token.AddReferenceToken(hashToken, context.ClientName, context.UserName);

                    var refreshToken = await IssueRefreshToken(token, context);

                    tokenResponse.AddRefreshToken(refreshToken);

                    await _tokenStore.CreateToken(token);

                    return tokenResponse;
                }
            default:
                _logger.LogError(
                    "Unsupported token type {TokenType} for client {ClientId}",
                    context.TokenType,
                    context.ClientId);

                throw new InvalidOperationException(
                    $"Unsupported token type '{context.TokenType}' for client '{context.ClientId}'.");
        }
    }

    private async Task<string?> IssueRefreshToken(Token token, TokenContext context)
    {
        var scopes = ResolveScopes(context.Scopes);

        var includeRefreshToken = scopes.Contains("offline_access");

        string refreshToken = string.Empty;

        if (!includeRefreshToken)
        {
            return default!;
        }

        _logger.LogInfo("Generating new refresh token.");

        context.SetRefreshTokenExpiry();

        var newRefreshToken = _tokenSecretGenerator.GenerateRawToken();

        var hashToken = _tokenSecretGenerator.HashToken(newRefreshToken);

        token.AddRefreshToken(context.RefreshExpiresAt, hashToken,
            _currentUserService.IpAddress!, context.ClientName, context.UserName);

        _logger.LogDebug("Created refresh token entity with expiry {Expiry}", context.RefreshExpiresAt);

        await _tokenStore.RemoveOldRefreshTokens(context.UserId, context.IpAddress, context.RefreshTokenExpiration);

        _logger.LogDebug("Removed old refresh tokens for user {UserId}", context.UserId);

        return newRefreshToken;
    }

    private TokenResponse CreateAccessToken(TokenContext tokenInfo, DateTime expiresAt, string tokenId)
    {
        _logger.LogDebug("Creating token (ID: {TokenId}) for {UserId} with roles: {Roles}",
            tokenId, tokenInfo.UserId, string.Join(",", tokenInfo.Roles));

        var accessToken = _tokenGenerator.CreateAccessToken(
            expiresAt,
            tokenId,
            tokenInfo.UserId.ToString(),
            tokenInfo.UserName,
            tokenInfo.TenantId.ToString(),
            tokenInfo.Audiences,
            tokenInfo.Scopes,
            tokenInfo.Roles);

        _logger.LogDebug("Token will expire at {ExpirationTime}", expiresAt);

        var idToken = CreateIDToken(tokenInfo);

        return TokenResponse.Create(tokenInfo.UserId, accessToken, expiresAt, idToken);
    }

    private TokenResponse CreateReferenceToken(TokenContext tokenInfo, DateTime expiresAt)
    {
        var token = Guid.NewGuid().ToString();

        _logger.LogDebug("Creating access token for user {UserId} with expiry {Expiry}",
            tokenInfo.UserId, expiresAt);

        _logger.LogDebug("Access token saved to database with ID: {TokenId}",
            $"{token.SubstringSafe(0, 5)}...");

        var idToken = CreateIDToken(tokenInfo);

        return TokenResponse.Create(tokenInfo.UserId, token, expiresAt, idToken);
    }

    private string CreateIDToken(TokenContext tokenInfo)
    {
        var tokenId = Guid.NewGuid().ToString();

        var expiresAt = DateTime.UtcNow.AddMinutes(tokenInfo.AccessTokenLifetime);

        _logger.LogDebug("Creating id token (ID: {TokenId}) for {UserId} ",
            tokenId, tokenInfo.UserId);

        var idToken = _tokenGenerator.CreateIDToken(
            expiresAt,
            tokenId,
            tokenInfo.UserId.ToString(),
            tokenInfo.UserName,
            new[] { tokenInfo.ClientId });

        _logger.LogDebug("Token will expire at {ExpirationTime}", expiresAt);

        return idToken;
    }

    private static HashSet<string> ResolveScopes(string[] clientScopes)
    {
        var scopes = clientScopes.Count() > 0 ? string.Join(' ', clientScopes) : string.Empty;

        if (string.IsNullOrWhiteSpace(scopes))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}