
using IDP.Core.Model;

namespace IDP.Core.OAuth.TokenHandlers;

internal interface ITokenGrantHandler
{
    Task<TokenResponse> HandleAsync(TokenRequest request);
}
