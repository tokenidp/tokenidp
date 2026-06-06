using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Core.Admin.Users.UseCases;

namespace TokenIDP.Core.Admin.Endpoints;

internal sealed class PasswordResetEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth")
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            PasswordResetUseCase passwordResetUseCase,
            HttpContext httpContext) =>
        {
            var command = new InitiateSelfServicePasswordResetCommand
            {
                Email = request.Email,
                ClientId = request.ClientId
            };

            var response = await passwordResetUseCase
                .InitiateSelfServicePasswordReset(command, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .AllowAnonymous()
        .WithName("SelfPasswordReset")
        .WithTags("PasswordReset");

        authGroup.MapPost("/reset-password", async (
           CompletePasswordResetRequest request,
           PasswordResetUseCase passwordResetUseCase,
           HttpContext httpContext) =>
        {
            var command = new CompletePasswordResetCommand
            {
                RawToken = request.Token,
                NewPassword = request.NewPassword
            };

            var response = await passwordResetUseCase
                .CompletePasswordReset(command, httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
       .AllowAnonymous()
       .WithName("CompletePasswordReset")
       .WithTags("PasswordReset");

        authGroup.MapPost("/forgot-password/form", async (
            HttpContext httpContext,
            IAntiforgery antiforgery,
            IAuthorizationRepository authorizationStore,
            PasswordResetUseCase passwordResetUseCase) =>
        {
            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var email = form["email"].ToString();
            var ctx = form["ctx"].ToString();
            var clientId = form["clientId"].ToString();

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            if (string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(ctx))
            {
                var preAuthorization = await authorizationStore.GetPreAuthorization(ctx);
                clientId = preAuthorization?.ClientId ?? string.Empty;
            }

            var response = await passwordResetUseCase.InitiateSelfServicePasswordReset(
                new InitiateSelfServicePasswordResetCommand
                {
                    Email = email,
                    ClientId = clientId
                },
                httpContext.RequestAborted);

            if (!response.IsSuccess)
            {
                return Results.Redirect(BuildForgotPasswordUrl(ctx, clientId, "Unable to process the request right now. Please try again."));
            }

            return Results.Redirect(BuildForgotPasswordUrl(ctx, clientId, success: true));
        })
        .AllowAnonymous()
        .WithName("SelfPasswordResetForm")
        .WithTags("PasswordReset");

        authGroup.MapPost("/reset-password/form", async (
            HttpContext httpContext,
            IAntiforgery antiforgery,
            PasswordResetUseCase passwordResetUseCase) =>
        {
            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var token = form["token"].ToString();
            var newPassword = form["newPassword"].ToString();
            var confirmPassword = form["confirmPassword"].ToString();

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                return Results.Redirect(BuildResetPasswordUrl(token, "Passwords do not match."));
            }

            var response = await passwordResetUseCase.CompletePasswordReset(
                new CompletePasswordResetCommand
                {
                    RawToken = token,
                    NewPassword = newPassword
                },
                httpContext.RequestAborted);

            if (!response.IsSuccess)
            {
                return Results.Redirect(BuildResetPasswordUrl(token, "Invalid or expired reset link."));
            }

            return Results.Redirect(BuildResetPasswordUrl(token, success: true));
        })
        .AllowAnonymous()
        .WithName("CompletePasswordResetForm")
        .WithTags("PasswordReset");
    }

    private static string BuildForgotPasswordUrl(
        string ctx,
        string clientId,
        string? errorDescription = null,
        bool success = false)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(ctx))
        {
            query["ctx"] = ctx;
        }

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            query["client_id"] = clientId;
        }

        if (!string.IsNullOrWhiteSpace(errorDescription))
        {
            query["error_description"] = errorDescription;
        }

        if (success)
        {
            query["sent"] = "1";
        }

        return QueryHelpers.AddQueryString("/forgot-password", query);
    }

    private static string BuildResetPasswordUrl(
        string token,
        string? errorDescription = null,
        bool success = false)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(token))
        {
            query["token"] = token;
        }

        if (!string.IsNullOrWhiteSpace(errorDescription))
        {
            query["error_description"] = errorDescription;
        }

        if (success)
        {
            query["reset"] = "1";
        }

        return QueryHelpers.AddQueryString("/reset-password", query);
    }
}

