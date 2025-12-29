using IDP.Core.Model;

namespace IDP.Core.OAuth;

public interface IAuthorizationCodeUseCase
{
    Task<AuthResponse> Authenticate(AuthRequest request);
    Task<AuthResponse> VerifyCode(MfaRequest request);
    Task<ClientValidationResult> ValidateClient(string clientId);
}
