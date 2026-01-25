namespace IDP.Core.Abstractions;

public interface IMfaUseCase
{
    Task<AuthResponse> GenerateMfaCode(AuthRequest request, int userId);

    Task<(AuthRequest?, AuthResponse)> VerifyMfaRequest(MfaRequest request);

    Task<AuthResponse> ResendMfaCode(MfaRequest request);
}
