using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.WebUtilities;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Users.UseCases;

namespace TokenIDP.Core.OAuth.Endpoints;

internal sealed class SignupEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/signup/form", async (HttpContext httpContext,
            IAntiforgery antiforgery,
            IAppLogger<SignupEndpoint> logger,
            IAuthorizationRepository authorizationStore,
            ITenantContextAccessor tenantContext,
            CreateAccountUseCase createAccountUseCase) =>
        {
            var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            var ctx = form["ctx"].ToString();

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning("Signup form rejected by antiforgery validation. Error={Error}", ex.Message);
                return Results.BadRequest("Invalid or missing antiforgery token.");
            }

            if (string.IsNullOrWhiteSpace(ctx))
            {
                return Results.Redirect(BuildSignupUrl(ctx, "Missing authorization context id."));
            }

            var preAuthorization = await authorizationStore.GetPreAuthorization(ctx);
            if (preAuthorization is null)
            {
                return Results.Redirect(BuildSignupUrl(ctx, "Authorization context is invalid or expired."));
            }

            var password = form["password"].ToString();
            var confirmPassword = form["confirmPassword"].ToString();

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return Results.Redirect(BuildSignupUrl(ctx, "Password mismatch."));
            }

            try
            {
                tenantContext.SetTenantId(preAuthorization.TenantId);
                tenantContext.SetClientId(preAuthorization.ClientId_FK);

                var result = await createAccountUseCase.Execute(new CreateAccountRequest
                {
                    FirstName = form["firstName"].ToString(),
                    LastName = form["lastName"].ToString(),
                    Email = form["email"].ToString(),
                    PhoneNumber = form["phoneNumber"].ToString(),
                    UserName = form["userName"].ToString(),
                    Password = password,
                    AuthorizationContextId = ctx
                }, httpContext.RequestAborted);

                if (!result.IsSuccess)
                {
                    return Results.Redirect(BuildSignupUrl(ctx, result.ErrorMessage ?? "Unable to create account."));
                }

                return Results.Redirect(BuildLoginUrl(ctx, created: true, verifyRequired: result.RequiresEmailVerification));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Signup form failed");
                return Results.Redirect(BuildSignupUrl(ctx, "An error occurred while creating the account."));
            }
            finally
            {
                tenantContext.Clear();
            }
        })
        .AllowAnonymous()
        .WithName("SignupForm")
        .WithTags("Signup");
    }

    private static string BuildSignupUrl(string ctx, string? errorDescription = null)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ctx"] = ctx
        };

        if (!string.IsNullOrWhiteSpace(errorDescription))
        {
            query["error_description"] = errorDescription;
        }

        return QueryHelpers.AddQueryString("/signup", query);
    }

    private static string BuildLoginUrl(string ctx, bool created, bool verifyRequired)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ctx"] = ctx
        };

        if (created)
        {
            query["created"] = "1";
        }

        if (verifyRequired)
        {
            query["verify"] = "1";
        }

        return QueryHelpers.AddQueryString("/login", query);
    }
}
