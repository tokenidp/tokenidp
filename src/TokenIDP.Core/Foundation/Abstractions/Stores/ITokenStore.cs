namespace TokenIDP.Core.Foundation.Abstractions.Stores;

public interface ITokenStore
{
    Task<int> CreateToken(Token token);

    Task<Token?> GetToken(byte[] tokenHash);

    Task<Token?> GetRefreshToken(byte[] tokenHash);

    Task<int> RevokeToken(Token token);

    Task<bool> RemoveOldRefreshTokens(int userId, string ipAddress, int expiry);
}

