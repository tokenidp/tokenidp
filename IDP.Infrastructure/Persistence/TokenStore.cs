using IDP.Domain.AggregateRoots;

namespace IDP.Infrastructure.Persistence;

internal class TokenStore : ITokenStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<TokenStore> _logger;

    public TokenStore(IApplicationDbContext dbContext,
        IAppLogger<TokenStore> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> CheckUniqueRefreshToken(string token)
    {
        _logger.LogTrace("Checking uniqueness of token");

        bool isUnique = await _dbContext.RefreshTokens
            .AnyAsync(t => t.Token == token);

        _logger.LogDebug("Token uniqueness check result: {IsUnique}", isUnique);

        return isUnique;
    }

    public async Task<int> CreateReferenceToken(ReferenceToken referenceToken)
    {
        _dbContext.ReferenceTokens.Add(referenceToken);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }

    public async Task<int> CreateRefreshToken(RefreshToken refreshToken)
    {
        _dbContext.RefreshTokens.Add(refreshToken);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }

    public async Task<ReferenceToken?> GetReferenceToken(string token)
    {
        var referenceToken = await _dbContext.ReferenceTokens
             .FirstOrDefaultAsync(s => s.Token == token && s.IsRevoked != true);

        return referenceToken;
    }

    public async Task<RefreshToken?> GetRefreshToken(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens.Where(t => t.Token == token)
            .FirstOrDefaultAsync();

        return refreshToken;
    }

    public async Task<bool> RemoveOldRefreshTokens(int userId, int expiry)
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

        return true;
    }

    public async Task<int> RevokeToken(ReferenceToken referenceToken)
    {
        _dbContext.ReferenceTokens.Update(referenceToken);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }
}
