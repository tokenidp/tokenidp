using IDP.Core.Model;

namespace IDP.Core.OAuth.Interfaces;

public interface IMfaService
{
    Task<AuthResponse> GenerateMfaCode(AuthRequest request, int userId);

    Task<(AuthRequest?, AuthResponse)> VerifyMfaRequest(MfaRequest request);

    Task<AuthResponse> ResendMfaCode(MfaRequest request);
}
