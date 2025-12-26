using IDP.Core.Common.Interfaces;
using IDP.Core.OAuth.Model;
using IDP.Core.Options;
using static IDP.Core.Domain.AggregateRoots.Clients.Client;

namespace IDP.Core.TokenServices;

internal class RefreshTokenService
{
    private readonly TokenOption _jwtSettings;
    private readonly ApplicationDbContext _dbContext;
    private readonly TokenServiceFactory _tokenServiceFactory;
    private readonly IAppLogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IOptions<TokenOption> jwtSettings,
        ApplicationDbContext dbContext,
        TokenServiceFactory tokenServiceFactory,
        IAppLogger<RefreshTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _dbContext = dbContext;
        _tokenServiceFactory = tokenServiceFactory;
        _logger = logger;
    }

    public async Task<TokenResponse> GenerateRefreshToken(string token, string clientId, string ipAddress)
    {
        _logger.LogInfo("Generating refresh token for client {ClientId} from {IPAddress}", clientId, ipAddress);

        var newRefreshToken = JwtTokenGenerator.CreateRefreshToken();
        _logger.LogDebug("Generated new refresh token (unverified uniqueness)");

        var user = await GetUserByRefreshToken(token);
        _logger.LogDebug("Found user {UserId} associated with refresh token", user.Id);

        if (await CheckUniqueToken(newRefreshToken))
        {
            _logger.LogWarning("Duplicate refresh token detected, regenerating...");
            return await GenerateRefreshToken(token, clientId, ipAddress);
        }

        var refreshToken = new RefreshToken(user.Id, newRefreshToken, ipAddress, _jwtSettings.RefreshTokenExpiry);
        _logger.LogDebug("Created refresh token entity with expiry {Expiry}", refreshToken.Expires);

        await RemoveOldRefreshTokens(user.Id);
        _logger.LogDebug("Removed old refresh tokens for user {UserId}", user.Id);

        user.RefreshTokens.Add(refreshToken);
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Successfully saved new refresh token for user {UserId}", user.Id);

        ITokenService _tokenService = _tokenServiceFactory.GetService(TokenType.JWT);
        var response = await _tokenService.GenerateToken(user.Id, user.TenantId, user.UserName, clientId);
        _logger.LogDebug("Generated new access token via token service for user {UserId}", user.Id);

        return response;
    }

    public async Task RevokeRefreshToken(string token, string ipAddress, string reason = null)
    {
        _logger.LogInfo("Revoking refresh token from {IPAddress}. Reason: {Reason}",
            ipAddress, reason ?? "unspecified");

        var user = await GetUserByRefreshToken(token);
        _logger.LogDebug("Found user {UserId} for token revocation", user.Id);

        var refreshToken = user.RefreshTokens.Single(x => x.Token == token);
        refreshToken.RevokeToken(ipAddress, reason);
        _logger.LogDebug("Marked token as revoked at {RevocationTime}", DateTime.UtcNow);

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Successfully revoked refresh token for user {UserId}", user.Id);
    }

    private async Task<bool> CheckUniqueToken(string token)
    {
        _logger.LogTrace("Checking uniqueness of token");
        bool isUnique = await _dbContext.RefreshTokens
            .AnyAsync(t => t.Token == token);

        _logger.LogDebug("Token uniqueness check result: {IsUnique}", isUnique);
        return isUnique;
    }

    private async Task<User> GetUserByRefreshToken(string token)
    {
        _logger.LogDebug("Looking up user by refresh token");

        var user = await _dbContext.RefreshTokens
            .Where(t => t.Token == token)
            .Join(_dbContext.Users,
                t => t.UserId,
                u => u.Id,
                (t, u) => u)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogWarning("No user found for refresh token");
            throw new NotFoundException("Refresh token not found.");
        }

        _logger.LogDebug("Found user {UserId} for token", user.Id);
        return user;
    }

    private async Task RemoveOldRefreshTokens(int userId)
    {
        _logger.LogDebug("Removing old refresh tokens for user {UserId}", userId);

        var cutoff = DateTime.UtcNow.AddDays(-_jwtSettings.RefreshTokenExpiry);

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
