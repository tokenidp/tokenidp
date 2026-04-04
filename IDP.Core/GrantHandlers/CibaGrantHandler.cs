namespace IDP.Core.GrantHandlers;

internal class CibaGrantHandler : ITokenGrantHandler
{
    public Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        throw new TokenRequestValidationException("unsupported_grant_type",
            "The ciba grant_type is not supported.");
    }
}
