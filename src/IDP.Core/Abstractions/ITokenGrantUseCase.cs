namespace IDP.Core.Abstractions;

public interface ITokenGrantUseCase
{
    Task<IResult> GetAccessToken(TokenRequest request);
}