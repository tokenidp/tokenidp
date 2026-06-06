using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;
using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.Endpoints;

public class DeviceFlowEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/device_authorization");

        authGroup.MapPost("", static async (DeviceAuthorizationRequest request,
            DeviceAuthorizationUseCase useCase) =>
        {
            var result = await useCase.CreateAsync(request, CancellationToken.None);

            return result;
        })
        .WithName("DeviceAuthorization")
        .WithTags("DeviceAuthorization");

        app.MapPost("/device/form", async (HttpContext httpContext,
            IAntiforgery antiforgery,
            IAppLogger<DeviceFlowEndpoint> logger,
            IDeviceAuthenticationUseCase authUseCase) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning("Device activation form rejected by antiforgery validation. Error={Error}", ex.Message);
                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var userCode = form["user_code"].ToString();
            var userName = form["username"].ToString();
            var password = form["password"].ToString();

            if (string.IsNullOrWhiteSpace(userCode) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password))
            {
                return Results.Redirect(BuildDeviceUrl(userCode, "User code, username, and password are required."));
            }

            try
            {
                var authResult = await authUseCase.AuthenticateAsync(userCode, userName, password);

                if (authResult?.IsSuccess == false)
                {
                    return Results.Redirect(BuildDeviceUrl(userCode, authResult.Error ?? "Invalid credentials."));
                }

                if (authResult?.TwoFactorEnabled == true)
                {
                    var mfaUrl = QueryHelpers.AddQueryString(
                        "/mfa",
                        new Dictionary<string, string?>(StringComparer.Ordinal)
                        {
                            ["uid"] = authResult.UserId.ToString(),
                            ["corid"] = authResult.CorrelationId,
                            ["flow"] = "device",
                            ["user_code"] = userCode
                        });

                    return Results.Redirect(mfaUrl);
                }

                var approvalResult = await authUseCase.ApproveAsync(userCode, authResult!.UserId);
                if (!approvalResult.IsSuccess)
                {
                    return Results.Redirect(BuildDeviceUrl(userCode, approvalResult.Error ?? "Invalid or expired code."));
                }

                return Results.Redirect(QueryHelpers.AddQueryString("/device", "approved", "1"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Device activation failed");
                return Results.Redirect(BuildDeviceUrl(userCode, "An unexpected error occurred."));
            }
        })
        .AllowAnonymous()
        .WithName("DeviceActivationForm")
        .WithTags("DeviceAuthorization");
    }

    private static string BuildDeviceUrl(string userCode, string? errorDescription = null)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(userCode))
        {
            query["user_code"] = userCode;
        }

        if (!string.IsNullOrWhiteSpace(errorDescription))
        {
            query["error_description"] = errorDescription;
        }

        return QueryHelpers.AddQueryString("/device", query);
    }
}

