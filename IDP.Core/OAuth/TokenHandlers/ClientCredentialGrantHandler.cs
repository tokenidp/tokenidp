using IDP.Core.Model;

namespace IDP.Core.OAuth.TokenHandlers;

internal class ClientCredentialGrantHandler : ITokenGrantHandler
{
    public Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        throw new NotImplementedException();
    }
}
