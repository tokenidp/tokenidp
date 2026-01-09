namespace IDP.Core.TokenHandlers;

internal interface ITokenGrantHandler
{
    Task<TokenResponse> HandleAsync(TokenRequest request);
}
