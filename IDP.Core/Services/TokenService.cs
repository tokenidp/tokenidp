using IDP.Domain.AggregateRoots;
using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Core.Services;

internal sealed class TokenService
{
    private readonly IAppLogger<TokenService> _logger;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ITokenStore _tokenStore;

    public TokenService(JwtTokenGenerator tokenGenerator,
        IAppLogger<TokenService> logger,
        ITokenStore tokenStore)
    {
        _tokenGenerator = tokenGenerator;
        _logger = logger;
        _tokenStore = tokenStore;
    }

    internal async Task<TokenResponse> CreateToken(TokenInfo tokenInfo)
    {
        switch (tokenInfo.AccessTokenType)
        {
            case TokenTypes.JWT:
                return CreateAccessToken(tokenInfo);

            case TokenTypes.ReferenceToken:
                return await CreateReferenceToken(tokenInfo);

            default:
                _logger.LogError(
                    "Unsupported token type {TokenType} for client {ClientId}",
                    tokenInfo.AccessTokenType,
                    tokenInfo.ClientId);

                throw new InvalidOperationException(
                    $"Unsupported token type '{tokenInfo.AccessTokenType}' for client '{tokenInfo.ClientId}'.");
        }
    }

    internal async Task<string> CreateRefreshToken(int userId, string ipAddress, int tokenExpiry)
    {
        var newRefreshToken = JwtTokenGenerator.CreateRefreshToken();

        _logger.LogDebug("Generated new refresh token (unverified uniqueness)");

        if (await _tokenStore.CheckUniqueRefreshToken(newRefreshToken))
        {
            _logger.LogWarning("Duplicate refresh token detected, regenerating...");

            await CreateRefreshToken(userId, ipAddress, tokenExpiry);
        }

        var refreshToken = new RefreshToken(
            userId,
            newRefreshToken,
            ipAddress,
            tokenExpiry);

        _logger.LogDebug("Created refresh token entity with expiry {Expiry}", refreshToken.Expires);

        await _tokenStore.RemoveOldRefreshTokens(userId, tokenExpiry);

        _logger.LogDebug("Removed old refresh tokens for user {UserId}", userId);

        await _tokenStore.CreateRefreshToken(refreshToken);

        return newRefreshToken;
    }

    private TokenResponse CreateAccessToken(TokenInfo tokenInfo)
    {
        var tokenId = Guid.NewGuid().ToString();

        var expiresAt = DateTime.UtcNow.AddMinutes(tokenInfo.AccessTokenLifetime);

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

    private async Task<TokenResponse> CreateReferenceToken(TokenInfo tokenInfo)
    {
        var token = Guid.NewGuid().ToString();

        var expiresAt = DateTime.UtcNow.AddMinutes(tokenInfo.AccessTokenLifetime);

        _logger.LogDebug("Creating access token for user {UserId} with expiry {Expiry}",
            tokenInfo.UserId, expiresAt);

        var referenceToken = new ReferenceToken(
            tokenInfo.UserId,
            tokenInfo.TenantId,
            tokenInfo.ClientId,
            token,
            string.Join(" ", tokenInfo.Scopes),
            expiresAt,
            DateTime.UtcNow,
            string.Join(",", tokenInfo.Roles),
            tokenInfo.UserId);

        await _tokenStore.CreateReferenceToken(referenceToken);

        _logger.LogDebug("Access token saved to database with ID: {TokenId}",
            $"{token.SubstringSafe(0, 5)}...");

        var idToken = CreateIDToken(tokenInfo);

        return TokenResponse.Create(tokenInfo.UserId, token, expiresAt, idToken);
    }

    private string CreateIDToken(TokenInfo tokenInfo)
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
}