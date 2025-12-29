using IDP.Core.Model;
using IDP.Core.OAuth;
using IDP.Core.TokenHandlers;

namespace IDP.Core.Endpoints;

internal class MfaEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/mfa");

        authGroup.MapPost("/verify", async (MfaRequest request,
            IAppLogger<MfaEndpoint> _logger,
            IAuthorizationCodeUseCase authenticationUseCase) =>
        {
            _logger.LogInfo("Mfa verification code started for user: {UserId}", request.UserId);

            var response = await authenticationUseCase.VerifyCode(request);

            _logger.LogInfo("Mfa completed for user: {UserId}", request.UserId);

            return response;
        })
        .WithName("VerifyMfa")
        .WithTags("VerifyMfa");

        authGroup.MapPost("/resend", async (MfaRequest request,
            IAppLogger<MfaEndpoint> _logger,
            MfaService mfaService) =>
        {
            _logger.LogInfo("Resend Mfa Code process started for user: {UserId}", request.UserId);

            var response = await mfaService.ResendMfaCode(request);

            _logger.LogInfo("Resend Mfa Code process completed for user: {UserId}", request.UserId);

            return response;
        })
        .WithName("ResendMfa")
        .WithTags("ResendMfa");
    }
}
