using IDP.Domain;
using IDP.Domain.AggregateRoots;

namespace IDP.Foundation.Abstractions;

public interface ITokenStore
{
    Task<int> CreateRefreshToken(RefreshToken refreshToken);

    Task<int> CreateReferenceToken(ReferenceToken referenceToken);

    Task<int> RevokeToken(ReferenceToken referenceToken);

    Task<bool> RemoveOldRefreshTokens(int userId, int expiry);

    Task<bool> CheckUniqueRefreshToken(string token);

    Task<RefreshToken?> GetRefreshToken(string token);

    Task<ReferenceToken?> GetReferenceToken(string token);
}
