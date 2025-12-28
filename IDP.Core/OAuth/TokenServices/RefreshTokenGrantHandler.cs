namespace IDP.Core.OAuth.TokenServices;

internal class RefreshTokenGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<RefreshTokenGrantHandler> _logger;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly ApplicationDbContext _dbContext;
    private readonly TokenService _tokenService;

    public RefreshTokenGrantHandler(JwtTokenGenerator tokenGenerator,
        IAppLogger<RefreshTokenGrantHandler> logger,
        TokenValidatorService tokenValidatorService,
        ApplicationDbContext dbContext,
        TokenService tokenService)
    {
        _logger = logger;
        _tokenValidatorService = tokenValidatorService;
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        _logger.LogInfo("Generating refresh token for client {ClientId} from {IPAddress}", request.ClientId, request.IpAddress);

        var newRefreshToken = JwtTokenGenerator.CreateRefreshToken();

        _logger.LogDebug("Generated new refresh token (unverified uniqueness)");

        if (await CheckUniqueToken(newRefreshToken))
        {
            _logger.LogWarning("Duplicate refresh token detected, regenerating...");

            await HandleAsync(request);
        }

        var existingRefreshToken = await _dbContext.RefreshTokens.Where(t => t.RefreshToken == request.RefreshToken)
                    .FirstOrDefaultAsync();

        if (existingRefreshToken == null)
        {
            _logger.LogWarning("Refresh token not found.");

            throw new NotFoundException("Refresh token not found.");
        }

        var tokenInfo = await _tokenValidatorService.ValidateTokenInfoAsync(request.ClientId, existingRefreshToken.UserId);

        var refreshToken = new UserRefreshToken(existingRefreshToken.UserId, newRefreshToken, request.IpAddress, tokenInfo.RefreshTokenExpiration);

        _logger.LogDebug("Created refresh token entity with expiry {Expiry}", refreshToken.Expires);

        await RemoveOldRefreshTokens(existingRefreshToken.UserId, tokenInfo.RefreshTokenExpiration);

        _logger.LogDebug("Removed old refresh tokens for user {UserId}", existingRefreshToken.UserId);

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();

        var token = _tokenService.CreateAccessToken(tokenInfo);

        token.AddRefreshToken(newRefreshToken);

        _logger.LogInfo("Successfully saved new refresh token for user {UserId}", existingRefreshToken.UserId);

        return token;
    }

    private async Task<bool> CheckUniqueToken(string token)
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
}
