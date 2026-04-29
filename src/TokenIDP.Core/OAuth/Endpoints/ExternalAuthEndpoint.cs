using TokenIDP.Core.OAuth.ExternalProviders.Abstractions;
using TokenIDP.Core.OAuth.ExternalProviders.Model;

namespace TokenIDP.Core.OAuth.Endpoints;

internal class ExternalAuthEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/external");

        group.MapGet("/{provider}/challenge", async (
            string provider,
            string ctx,
            IExternalAuthUseCase useCase,
            HttpContext httpContext) =>
        {
            if (!TryParseProvider(provider, out var providerType))
            {
                return Results.BadRequest("Invalid provider.");
            }

            if (string.IsNullOrWhiteSpace(ctx))
            {
                return Results.BadRequest("Missing authorization context id.");
            }

            var result = await useCase.StartChallengeAsync(
                providerType,
                ctx,
                httpContext.RequestAborted);

            return Results.Redirect(result.RedirectUrl);
        });

        group.MapGet("/{provider}/callback", async (
            string provider,
            string code,
            string state,
            IExternalAuthUseCase useCase,
            HttpContext httpContext) =>
        {
            if (!TryParseProvider(provider, out var providerType))
            {
                return Results.BadRequest("Invalid provider.");
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                return Results.BadRequest("Missing code/state.");
            }

            var input = new ExternalAuthCallbackInput(
                providerType,
                code,
                state);

            var result = await useCase.HandleCallbackAsync(input, httpContext.RequestAborted);

            return Results.Redirect(result.ResumeAuthorizeUrl);
        });
    }

    private static bool TryParseProvider(string value, out ExternalProviderTypes provider)
    {
        return Enum.TryParse(value, true, out provider);
    }
}

