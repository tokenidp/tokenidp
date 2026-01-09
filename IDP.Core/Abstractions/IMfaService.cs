namespace IDP.Core.Abstractions;

public interface IMfaService
{
    Task<AuthResponse> GenerateMfaCode(AuthRequest request, int userId);

    Task<(AuthRequest?, AuthResponse)> VerifyMfaRequest(MfaRequest request);

    Task<AuthResponse> ResendMfaCode(MfaRequest request);
}
