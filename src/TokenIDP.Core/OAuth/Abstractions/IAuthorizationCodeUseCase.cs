namespace TokenIDP.Core.OAuth.Abstractions;

public interface IAuthorizationCodeUseCase
{
    Task<AuthorizationResponse> Authenticate(AuthorizationRequest request);

    Task<TokenContext> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest);

    Task<AuthorizationResponse> GenerateAuthorizationCode(
        AuthorizationRequest request,
        int userId);
}
