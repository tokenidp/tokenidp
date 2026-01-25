using IDP.Domain.AggregateRoots.Tokens;

namespace IDP.Foundation.Abstractions.Stores;

public interface ITokenStore
{
    Task<int> CreateToken(Token token);

    Task<int> RevokeToken(Token token);

    Task<bool> RemoveOldRefreshTokens(int userId, int expiry);

    Task<Token?> GetReferenceToken(byte[] tokenHash);

    Task<Token?> GetRefreshToken(byte[] tokenHash);
}
