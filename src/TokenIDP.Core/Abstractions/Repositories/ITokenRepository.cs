using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Tokens;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface ITokenRepository
{
    Task<int> CreateToken(Token token);

    Task<Token?> GetToken(byte[] tokenHash);

    Task<Token?> GetRefreshToken(byte[] tokenHash);

    Task<int> RevokeToken(Token token);

    Task<bool> RemoveOldRefreshTokens(int userId, string ipAddress, int expiry);
    Task<Token?> GetActiveTokenAsync(Guid tokenId, int tenantId, CancellationToken ct);
    Task<PaginatedList<TokenListItem>> SearchTokensAsync(int tenantId, SearchData request, CancellationToken ct);
    Task<TokenDetail?> GetTokenDetailAsync(int tenantId, Guid tokenId, CancellationToken ct);
    Task<TokenLookups> GetTokenLookupsAsync(int tenantId, CancellationToken ct);
    Task<int> SaveAsync(Token token, CancellationToken ct);
    Task<int> RevokeActiveTokensForUserAsync(
        int tenantId,
        int userId,
        string reason,
        string ipAddress,
        int revokedByUserId,
        CancellationToken ct);
}

