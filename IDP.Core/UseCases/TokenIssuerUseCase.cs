using IDP.Domain.AggregateRoots.Clients;
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

        _logger.LogDebug("Token Expire At: {ExpiresAt}", context.ExpiresAt);
        _logger.LogDebug("Access Token Lifetime: {AccessTokenLifetime}", context.AccessTokenLifetime);

        var token = Token.CreateToken(context);

        var isClientCredentials = Enum.Equals(context.GrantType, GrantTypes.client_credentials);

        switch (context.TokenType)
        {
            case TokenTypes.JWT:
                {
                    var tokenResponse = CreateAccessToken(context, token.ExpiresAt, token.Id.ToString());

                    if (!isClientCredentials)
                    {
                        var refreshToken = await IssueRefreshToken(token, context);

                        tokenResponse.AddRefreshToken(refreshToken);
                    }

                    token.IssueJwt(context.ClientName);

                    await _tokenStore.CreateToken(token);

                    return tokenResponse;
                }
            case TokenTypes.ReferenceToken:
                {
                    var tokenResponse = CreateReferenceToken(context, token.ExpiresAt);

                    var hashToken = _tokenSecretGenerator.HashToken(tokenResponse.AccessToken);

                    token.AddReferenceToken(hashToken, context.ClientName, context.UserName);

                    if (!isClientCredentials)
                    {
                        var refreshToken = await IssueRefreshToken(token, context);

                        tokenResponse.AddRefreshToken(refreshToken);
                    }

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

        await _tokenStore.RemoveOldRefreshTokens(context.UserId ?? 0, context.IpAddress, context.RefreshTokenExpiration);

        _logger.LogDebug("Removed old refresh tokens for client {clientId}", context.ClientId);

        return newRefreshToken;
    }

    private TokenResponse CreateAccessToken(TokenContext tokenInfo, DateTime expiresAt, string tokenId)
    {
        _logger.LogDebug("Creating token for client {clientId}", tokenInfo.ClientId);

        var accessToken = _tokenGenerator.CreateAccessToken(
            expiresAt,
            tokenId,
            tokenInfo.ClientId,
            tokenInfo.UserId,
            tokenInfo.UserName,
            tokenInfo.TenantId.ToString(),
            tokenInfo.Audiences,
            tokenInfo.Scopes,
            tokenInfo.Roles);

        _logger.LogDebug("Token will expire at {ExpirationTime}", expiresAt);

        var idToken = CreateIDToken(tokenInfo);

        return TokenResponse.Success(tokenInfo.UserId, accessToken, tokenInfo.AccessTokenLifetime, expiresAt, idToken);
    }

    private TokenResponse CreateReferenceToken(TokenContext tokenInfo, DateTime expiresAt)
    {
        var token = Guid.NewGuid().ToString();

        _logger.LogDebug("Creating refresh token for user {UserId} with expiry {Expiry}",
            tokenInfo.UserId ?? 0, expiresAt);

        _logger.LogDebug("Access token saved to database with ID: {TokenId}",
            $"{token.SubstringSafe(0, 5)}...");

        var idToken = CreateIDToken(tokenInfo);

        return TokenResponse.Success(tokenInfo.UserId, token, tokenInfo.AccessTokenLifetime, expiresAt, idToken);
    }

    private string? CreateIDToken(TokenContext context)
    {
        var isClientCredentials = Enum.Equals(context.GrantType, GrantTypes.client_credentials);

        if (isClientCredentials)
        {
            return default;
        }
        var tokenId = Guid.NewGuid().ToString();

        var expiresAt = DateTime.UtcNow.AddMinutes(context.AccessTokenLifetime);

        _logger.LogDebug("Creating id token (ID: {TokenId}) ", tokenId);

        var idToken = _tokenGenerator.CreateIDToken(
            expiresAt,
            tokenId,
            context.ClientId,
            context.UserId,
            context.UserName,
            new[] { context.ClientId });

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