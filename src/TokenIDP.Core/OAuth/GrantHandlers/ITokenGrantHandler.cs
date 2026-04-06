namespace TokenIDP.Core.OAuth.GrantHandlers;

internal interface ITokenGrantHandler
{
    Task<TokenResponse> HandleAsync(TokenRequest request);
}

