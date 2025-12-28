namespace IDP.Core.OAuth.TokenServices;

internal class ClientCredentialGrantHandler : ITokenGrantHandler
{
    public Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        throw new NotImplementedException();
    }
}
