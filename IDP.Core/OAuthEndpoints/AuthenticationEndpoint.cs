using IDP.Core.OAuth.Model;
using IDP.Core.TokenServices.UseCases;

namespace IDP.Core.OAuthEndpoints;

internal class AuthenticationEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth");

        authGroup.MapPost("/login", async (AuthRequest request,
            IAppLogger<AuthenticationEndpoint> _logger,
            AuthenticationUseCase authenticationUseCase) =>
        {
            _logger.LogInfo("Authenticate called for user: {Username}", request.UserName);

            var response = await authenticationUseCase.Authenticate(request);

            _logger.LogInfo("Authenticate completed for user: {Username}", request.UserName);

            return response;
        }).WithName("Authenticate")
        .WithTags("Authentication");

        authGroup.MapPost("/verify-mfa", async (MfaRequest request,
            IAppLogger<AuthenticationEndpoint> _logger,
            AuthenticationUseCase authenticationUseCase) =>
        {
            _logger.LogInfo("Mfa verification code started for user: {UserId}", request.UserId);

            var response = await authenticationUseCase.VerifyCode(request);

            _logger.LogInfo("Mfa completed for user: {UserId}", request.UserId);

            return response;
        })
        .WithName("VerifyMfa")
        .WithTags("VerifyMfa");

        authGroup.MapPost("/resend-mfa", async (MfaRequest request,
            IAppLogger<AuthenticationEndpoint> _logger,
            AuthenticationUseCase authenticationUseCase) =>
        {
            _logger.LogInfo("Resend Mfa Code process started for user: {UserId}", request.UserId);

            var response = await authenticationUseCase.ResendMfaCode(request);

            _logger.LogInfo("Resend Mfa Code process completed for user: {UserId}", request.UserId);

            return response;
        })
        .WithName("ResendMfa")
        .WithTags("ResendMfa");
    }
}
