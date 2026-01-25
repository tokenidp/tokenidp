using IDP.Foundation.Abstractions.Stores;

namespace IDP.Infrastructure.Persistence;

internal class TokenStore : ITokenStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<TokenStore> _logger;

    public TokenStore(IApplicationDbContext dbContext,
        IAppLogger<TokenStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> CreateToken(Token token)
    {
        _dbContext.Tokens.Add(token);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }

    public async Task<Token?> GetReferenceToken(byte[] tokenHash)
    {
        var referenceToken = await _dbContext.Tokens
             .FirstOrDefaultAsync(s => s.ReferenceToken.TokenHash == tokenHash && s.IsRevoked != true);

        return referenceToken;
    }

    public async Task<Token?> GetRefreshToken(byte[] tokenHash)
    {
        var refreshToken = await _dbContext.Tokens
            .FirstOrDefaultAsync(t => t.RefreshToken.TokenHash == tokenHash && t.IsRevoked != true);

        return refreshToken;
    }

    public async Task<bool> RemoveOldRefreshTokens(int userId, int expiry)
    {
        _logger.LogDebug("Removing old refresh tokens for user {UserId}", userId);

        var cutoff = DateTime.UtcNow.AddDays(-expiry);

        var oldTokens = await _dbContext.Tokens
            .Where(t => t.UserId == userId && t.ExpiresAt < cutoff)
            .ToListAsync();

        if (oldTokens.Any())
        {
            foreach(var token in oldTokens)
            {
                token.Revoke(RevokeReason.RefreshReuse.ToString(), userId);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Removed {Count} old refresh tokens for user {UserId}",
                oldTokens.Count, userId);
        }
        else
        {
            _logger.LogDebug("No old refresh tokens to remove for user {UserId}", userId);
        }

        return true;
    }

    public async Task<int> RevokeToken(Token token)
    {
        _dbContext.Tokens.Update(token);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }
}
