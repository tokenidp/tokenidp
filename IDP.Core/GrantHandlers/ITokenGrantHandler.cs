namespace IDP.Core.GrantHandlers;

internal interface ITokenGrantHandler
{
    Task<TokenResponse> HandleAsync(TokenRequest request);
}
