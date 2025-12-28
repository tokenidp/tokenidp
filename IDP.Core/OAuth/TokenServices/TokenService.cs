namespace IDP.Core.OAuth.TokenServices;

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

    public TokenResponse CreateAccessToken(TokenInfo tokenInfo)
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

        return TokenResponse.Create(tokenInfo.UserId, accessToken, expiresAt, string.Empty);
    }

    public async Task<TokenResponse> CreateReferenceToken(TokenInfo tokenInfo)
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

        return TokenResponse.Create(tokenInfo.UserId, token, expiresAt, string.Empty);
    }
}
