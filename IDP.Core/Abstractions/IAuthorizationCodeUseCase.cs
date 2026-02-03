namespace IDP.Core.Abstractions;

public interface IAuthorizationCodeUseCase
{
    Task<AuthorizationResponse> Authenticate(AuthorizationRequest request);

    Task<AuthorizationResponse> VerifyMfaCode(MfaRequest request);

    Task<TokenContext> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest);
}