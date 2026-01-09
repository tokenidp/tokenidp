namespace IDP.Core.TokenHandlers;

internal sealed class TokenGrantFactory
{
    private readonly Func<GrantTypes, ITokenGrantHandler> _tokenGrantHandler;

    public TokenGrantFactory(Func<GrantTypes, ITokenGrantHandler> tokenGrantHandler)
    {
        _tokenGrantHandler = tokenGrantHandler;
    }

    internal ITokenGrantHandler GetService(GrantTypes grantType)
    {
        return _tokenGrantHandler(grantType);
    }
}