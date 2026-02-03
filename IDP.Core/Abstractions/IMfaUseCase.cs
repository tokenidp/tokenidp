namespace IDP.Core.Abstractions;

public interface IMfaUseCase
{
    Task<AuthorizationResponse> GenerateMfaCode(AuthorizationRequest request, int userId);

    Task<(AuthorizationRequest?, AuthorizationResponse)> VerifyMfaRequest(MfaRequest request);

    Task<AuthorizationResponse> ResendMfaCode(MfaRequest request);
}
