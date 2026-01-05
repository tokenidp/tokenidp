using IDP.Core.Model;
using IDP.Core.OAuth.Interfaces;

namespace IDP.Core.Endpoints;

internal class MfaEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/mfa");

        authGroup.MapPost("/verify", async (MfaRequest request,
            IAppLogger<MfaEndpoint> _logger,
            IMfaService mfaService) =>
        {
            _logger.LogInfo("Mfa verification code started for user: {UserId}", request.UserId);

            var (authRequest, authResponse) = await mfaService.VerifyMfaRequest(request);

            _logger.LogInfo("Mfa completed for user: {UserId}", request.UserId);

            return ApiResult<AuthResponse>.Success(authResponse);
        })
        .WithName("VerifyMfa")
        .WithTags("VerifyMfa");

        authGroup.MapPost("/resend", async (MfaRequest request,
            IAppLogger<MfaEndpoint> _logger,
            IMfaService mfaService) =>
        {
            _logger.LogInfo("Resend Mfa Code process started for user: {UserId}", request.UserId);

            if (string.IsNullOrEmpty(request.CorrelationId))
            {
                var errorResult = ApiResult<ApiError>.Failure(
                                ApiError.Failure("Correlation Id cannot be empty."));

                return Results.BadRequest(errorResult);
            }

            var response = await mfaService.ResendMfaCode(request);

            _logger.LogInfo("Resend Mfa Code process completed for user: {UserId}", request.UserId);

            return Results.Ok(ApiResult<AuthResponse>.Success(response));
        })
        .WithName("ResendMfa")
        .WithTags("ResendMfa");
    }
}
