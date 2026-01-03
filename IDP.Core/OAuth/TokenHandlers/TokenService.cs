using IDP.Core.Model;

namespace IDP.Core.OAuth.TokenHandlers;

internal sealed class TokenService
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

    public async Task<string> CreateRefreshToken(int userId, string ipAddress, int tokenExpiry)
    {
        var newRefreshToken = JwtTokenGenerator.CreateRefreshToken();

        _logger.LogDebug("Generated new refresh token (unverified uniqueness)");

        if (await CheckUniqueRefreshToken(newRefreshToken))
        {
            _logger.LogWarning("Duplicate refresh token detected, regenerating...");

            await CreateRefreshToken(userId, ipAddress, tokenExpiry);
        }

        var refreshToken = new UserRefreshToken(
            userId,
            newRefreshToken,
            ipAddress,
            tokenExpiry);

        _logger.LogDebug("Created refresh token entity with expiry {Expiry}", refreshToken.Expires);

        await RemoveOldRefreshTokens(userId, tokenExpiry);

        _logger.LogDebug("Removed old refresh tokens for user {UserId}", userId);

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();

        return newRefreshToken;
    }

    private async Task<bool> CheckUniqueRefreshToken(string token)
    {
        _logger.LogTrace("Checking uniqueness of token");

        bool isUnique = await _dbContext.RefreshTokens
            .AnyAsync(t => t.RefreshToken == token);

        _logger.LogDebug("Token uniqueness check result: {IsUnique}", isUnique);

        return isUnique;
    }

    private async Task RemoveOldRefreshTokens(int userId, int expiry)
    {
        _logger.LogDebug("Removing old refresh tokens for user {UserId}", userId);

        var cutoff = DateTime.UtcNow.AddDays(-expiry);

        var oldTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.Expires < cutoff)
            .ToListAsync();

        if (oldTokens.Any())
        {
            _dbContext.RefreshTokens.RemoveRange(oldTokens);

            await _dbContext.SaveChangesAsync();

            _logger.LogInfo("Removed {Count} old refresh tokens for user {UserId}",
                oldTokens.Count, userId);
        }
        else
        {
            _logger.LogDebug("No old refresh tokens to remove for user {UserId}", userId);
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