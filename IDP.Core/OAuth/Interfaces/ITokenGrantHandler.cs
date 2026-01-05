using IDP.Core.Model;

namespace IDP.Core.OAuth.Interfaces;

internal interface ITokenGrantHandler
{
    Task<TokenResponse> HandleAsync(TokenRequest request);
}
