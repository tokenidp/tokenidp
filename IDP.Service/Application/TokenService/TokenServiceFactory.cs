namespace IDP.Service.Application.TokenService;

public class TokenServiceFactory
{
    private readonly Func<TokenType, ITokenService> _tokenService;

    public TokenServiceFactory(Func<TokenType, ITokenService> tokenService)
    {
        _tokenService = tokenService;
    }

    public ITokenService GetService(TokenType tokenType)
    {
        return _tokenService(tokenType);
    }
}
