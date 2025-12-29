using IDP.Core.Model;

namespace IDP.Core.OAuth.TokenHandlers;

internal class TokenService
{
    private readonly IAppLogger<TokenService> _logger;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ApplicationDbContext _dbContext;

    public TokenService(JwtTokenGenerator tokenGenerator,
        IAppLogger<TokenService> logger,
        ApplicationDbContext dbContext)
    {
        _tokenGenerator = tokenGenerator;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<TokenResponse> CreateToken(TokenInfo tokenInfo)
    {
        switch (tokenInfo.AccessTokenType)
        {
            case TokenType.JWT:
                return CreateAccessToken(tokenInfo);

            case TokenType.ReferenceToken:
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

        var accessToken = new UserAccessToken(
            tokenInfo.UserId,
            tokenInfo.TenantId,
            tokenInfo.ClientId,
            token,
            string.Join(" ", tokenInfo.Scopes),
            expiresAt,
            DateTime.UtcNow,
            string.Join(",", tokenInfo.Roles),
            tokenInfo.UserId);

        _dbContext.UserAccessToken.Add(accessToken);

        await _dbContext.SaveChangesAsync();

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