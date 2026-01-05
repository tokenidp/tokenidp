using IDP.Core.Model;
using IDP.Core.OAuth.Interfaces;

namespace IDP.Core.OAuth.TokenHandlers;

internal class CibaGrantHandler : ITokenGrantHandler
{
    public Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        throw new NotImplementedException();
    }
}
