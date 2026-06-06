using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;

namespace TokenIDP.Core.OAuth.Endpoints;

internal class MfaEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/mfa");

        authGroup.MapPost("/verify", async (MfaRequest request,
            IAppLogger<MfaEndpoint> _logger,
            IMfaUseCase mfaUseCase) =>
        {
            _logger.LogInfo("Mfa verification code started for user: {UserId}", request.UserId);

            var (authRequest, authResponse) = await mfaUseCase.VerifyMfaRequest(request);

            _logger.LogInfo("Mfa completed for user: {UserId}", request.UserId);

            return ApiResult<AuthorizationResponse>.Success(authResponse);
        })
        .WithName("VerifyMfa")
        .WithTags("Mfa");

        authGroup.MapPost("/resend", async (MfaRequest request,
            IAppLogger<MfaEndpoint> _logger,
            IMfaUseCase mfaUseCase) =>
        {
            _logger.LogInfo("Resend Mfa Code process started for user: {UserId}", request.UserId);

            if (string.IsNullOrEmpty(request.CorrelationId))
            {
                var errorResult = ApiResult<ApiError>.Failure(
                                ApiError.Failure("Correlation Id cannot be empty."));

                return Results.BadRequest(errorResult);
            }

            var response = await mfaUseCase.ResendMfaCode(request);

            _logger.LogInfo("Resend Mfa Code process completed for user: {UserId}", request.UserId);

            return Results.Ok(ApiResult<AuthorizationResponse>.Success(response));
        })
        .WithName("ResendMfa")
        .WithTags("Mfa");

        authGroup.MapPost("/verify/form", async (HttpContext httpContext,
            IAntiforgery antiforgery,
            IAppLogger<MfaEndpoint> logger,
            IMfaUseCase mfaUseCase,
            IDeviceAuthenticationUseCase deviceAuthenticationUseCase) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning("MFA form rejected by antiforgery validation. Error={Error}", ex.Message);
                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var uid = form["uid"].ToString();
            var corid = form["corid"].ToString();
            var flow = form["flow"].ToString();
            var userCode = form["user_code"].ToString();
            var code = form["code"].ToString();
            var mfaUrl = BuildMfaUrl(uid, corid, flow, userCode);

            if (!int.TryParse(uid, out var userId) ||
                string.IsNullOrWhiteSpace(corid) ||
                code.Length != 6 ||
                !code.All(char.IsDigit))
            {
                return Results.Redirect(BuildMfaUrl(uid, corid, flow, userCode, "Enter a valid 6-digit code."));
            }

            var request = new MfaRequest
            {
                UserId = userId,
                CorrelationId = corid,
                Code = code
            };

            var (_, authResponse) = await mfaUseCase.VerifyMfaRequest(request);

            if (authResponse?.IsSuccess != true)
            {
                return Results.Redirect(BuildMfaUrl(
                    uid,
                    corid,
                    flow,
                    userCode,
                    authResponse?.Error ?? "Unable to verify the code. Please try again."));
            }

            if (string.Equals(flow, "device", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(userCode))
                {
                    return Results.Redirect(BuildMfaUrl(uid, corid, flow, userCode, "Device user code is missing."));
                }

                var approval = await deviceAuthenticationUseCase.ApproveAsync(userCode, userId);
                if (!approval.IsSuccess)
                {
                    return Results.Redirect(BuildMfaUrl(
                        uid,
                        corid,
                        flow,
                        userCode,
                        approval.Error ?? "Invalid or expired code."));
                }

                return Results.Redirect(QueryHelpers.AddQueryString("/device", "approved", "1"));
            }

            return Results.Redirect(QueryHelpers.AddQueryString("/authorize", "ctx", corid));
        })
        .AllowAnonymous()
        .WithName("VerifyMfaForm")
        .WithTags("Mfa");

        authGroup.MapPost("/resend/form", async (HttpContext httpContext,
            IAntiforgery antiforgery,
            IAppLogger<MfaEndpoint> logger,
            IMfaUseCase mfaUseCase) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning("MFA resend form rejected by antiforgery validation. Error={Error}", ex.Message);
                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var uid = form["uid"].ToString();
            var corid = form["corid"].ToString();
            var flow = form["flow"].ToString();
            var userCode = form["user_code"].ToString();

            if (!int.TryParse(uid, out var userId) || string.IsNullOrWhiteSpace(corid))
            {
                return Results.Redirect(BuildMfaUrl(uid, corid, flow, userCode, "Correlation Id cannot be empty."));
            }

            var response = await mfaUseCase.ResendMfaCode(new MfaRequest
            {
                UserId = userId,
                CorrelationId = corid
            });

            if (response?.IsSuccess != true)
            {
                return Results.Redirect(BuildMfaUrl(
                    uid,
                    corid,
                    flow,
                    userCode,
                    response?.Error ?? "Unable to resend the code. Please try again."));
            }

            return Results.Redirect(BuildMfaUrl(uid, corid, flow, userCode, resent: true));
        })
        .AllowAnonymous()
        .WithName("ResendMfaForm")
        .WithTags("Mfa");
    }

    private static string BuildMfaUrl(
        string uid,
        string corid,
        string flow,
        string userCode,
        string? errorDescription = null,
        bool resent = false)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["uid"] = uid,
            ["corid"] = corid,
            ["flow"] = flow
        };

        if (!string.IsNullOrWhiteSpace(userCode))
        {
            query["user_code"] = userCode;
        }

        if (!string.IsNullOrWhiteSpace(errorDescription))
        {
            query["error_description"] = errorDescription;
        }

        if (resent)
        {
            query["resent"] = "1";
        }

        return QueryHelpers.AddQueryString("/mfa", query);
    }
}

