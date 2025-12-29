namespace IDP.Core.OAuth.TokenHandlers;

internal class TokenGrantFactory
{
    private readonly Func<GrantType, ITokenGrantHandler> _tokenGrantHandler;

    public TokenGrantFactory(Func<GrantType, ITokenGrantHandler> tokenGrantHandler)
    {
        _tokenGrantHandler = tokenGrantHandler;
    }

    public ITokenGrantHandler GetService(GrantType grantType)
    {
        return _tokenGrantHandler(grantType);
    }
}