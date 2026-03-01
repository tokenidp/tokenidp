using IDP.Domain.AggregateRoots.Tokens;
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

    public async Task<Token?> GetToken(byte[] tokenHash)
    {
        var token = await _dbContext.Tokens.FirstOrDefaultAsync(s =>
        !s.IsRevoked &&
        s.ExpiresAt > DateTime.UtcNow &&
        (
            (s.RefreshToken != null && s.RefreshToken.TokenHash == tokenHash) ||
            (s.ReferenceToken != null && s.ReferenceToken.TokenHash == tokenHash)
        ));

        return token;
    }

    public async Task<Token?> GetRefreshToken(byte[] tokenHash)
    {
        var refreshToken = await _dbContext.Tokens
            .FirstOrDefaultAsync(t => t.RefreshToken.TokenHash == tokenHash && t.IsRevoked != true);

        return refreshToken;
    }

    public async Task<bool> RemoveOldRefreshTokens(int userId, string ipAddress, int expiry)
    {
        _logger.LogDebug("Removing old refresh tokens for user {UserId}", userId);

        var cutoff = DateTime.UtcNow.AddDays(-expiry);

        var oldTokens = await _dbContext.Tokens
            .Where(t => t.UserId == userId && t.ExpiresAt < cutoff)
            .ToListAsync();

        if (oldTokens.Any())
        {
            foreach (var token in oldTokens)
            {
                token.Revoke(RevokeReason.RefreshReuse.ToString(), ipAddress, userId);
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
