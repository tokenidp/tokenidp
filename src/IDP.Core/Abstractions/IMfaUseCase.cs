namespace IDP.Core.Abstractions;

public interface IMfaUseCase
{
    Task<AuthorizationResponse> GenerateMfaForAuthorizeAsync(AuthorizationRequest request,
        int userId,
        CancellationToken ct = default);

    Task<AuthorizationResponse> GenerateMfaCode(GenerateMfaCommand command,
        CancellationToken ct = default);

    Task<(AuthorizationRequest?, AuthorizationResponse)> VerifyMfaRequest(MfaRequest request);

    Task<AuthorizationResponse> ResendMfaCode(MfaRequest request);
}
