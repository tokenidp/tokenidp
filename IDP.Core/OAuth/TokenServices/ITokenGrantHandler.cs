
namespace IDP.Core.OAuth.TokenServices;

internal interface ITokenGrantHandler
{
    Task<TokenResponse> HandleAsync(TokenRequest request);
}
