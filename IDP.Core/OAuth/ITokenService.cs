using IDP.Core.OAuth.Model;

namespace IDP.Core.TokenServices;

internal interface ITokenService
{
    Task<TokenResponse> GenerateToken(TokenRequest tokenRequest, string ipAddress);
    Task<TokenResponse> GenerateToken(int userId, int tenantId, string userName, string clientId);
}
