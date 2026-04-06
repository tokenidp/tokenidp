namespace TokenIDP.Core.OAuth.Abstractions;

public interface ITokenGrantUseCase
{
    Task<IResult> GetAccessToken(TokenRequest request);
}
