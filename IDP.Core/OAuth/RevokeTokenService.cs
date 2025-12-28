namespace IDP.Core.OAuth;

internal class RevokeTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAppLogger<RevokeTokenService> _logger;

    public RevokeTokenService(
        ApplicationDbContext dbContext,
        IAppLogger<RevokeTokenService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RevokeToken(RevokeTokenRequest request)
    {
        var existingRefreshToken = await _dbContext.RefreshTokens.Where(t => t.RefreshToken == request.RefreshToken)
            .FirstOrDefaultAsync();

        if (existingRefreshToken == null)
        {
            _logger.LogWarning("Refresh token not found.");

            throw new NotFoundException("Refresh token not found.");
        }

        _logger.LogDebug("Refresh token found for {UserId} for token revocation", existingRefreshToken.UserId);

        existingRefreshToken.RevokeToken(request.IpAddress, request.ReasonRevoked);

        _logger.LogDebug("Marked token as revoked at {RevocationTime}", DateTime.UtcNow);

        await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Successfully revoked refresh token for user {UserId}", existingRefreshToken.Id);
    }
}
