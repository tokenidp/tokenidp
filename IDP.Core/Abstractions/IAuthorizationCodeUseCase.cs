namespace IDP.Core.Abstractions;

public interface IAuthorizationCodeUseCase
{
    Task<AuthResponse> Authenticate(AuthRequest request);

    Task<AuthResponse> VerifyMfaCode(MfaRequest request);

    Task<TokenContext> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest);
}