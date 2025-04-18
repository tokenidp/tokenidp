namespace IDP.Service.Application.TokenService;

public interface ITokenService
{
    Task<TokenResponse> GenerateToken(TokenRequest tokenRequest, string ipAddress);
    Task<TokenResponse> GenerateToken(int userId, int tenantId, string userName, string clientId);
}
