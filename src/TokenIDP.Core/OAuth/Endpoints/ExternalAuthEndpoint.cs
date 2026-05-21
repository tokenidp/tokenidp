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
                CancellationToken.None);

            return Results.Redirect(result.RedirectUrl);
        });

        group.MapGet("/{provider}/callback", async (
            string provider,
            string? code,
            string? state,
            string? error,
            string? error_description,
            IExternalAuthUseCase useCase,
            IAppLogger<ExternalAuthEndpoints> logger,
            HttpContext httpContext) =>
        {
            if (!TryParseProvider(provider, out var providerType))
            {
                return Results.BadRequest("Invalid provider.");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                logger.LogWarning(
                    "External authentication provider returned an error. Provider={Provider}, Error={Error}, Description={Description}",
                    providerType,
                    error,
                    error_description ?? string.Empty);

                return Results.BadRequest("External authentication provider returned an error.");
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                logger.LogWarning(
                    "External authentication callback is missing code or state. Provider={Provider}, HasCode={HasCode}, HasState={HasState}, Path={Path}, QueryString={QueryString}",
                    providerType,
                    !string.IsNullOrWhiteSpace(code),
                    !string.IsNullOrWhiteSpace(state),
                    httpContext.Request.Path.Value ?? string.Empty,
                    httpContext.Request.QueryString.Value ?? string.Empty);

                return Results.BadRequest("Missing code/state.");
            }

            var input = new ExternalAuthCallbackInput(
                providerType,
                code,
                state);

            var result = await useCase.HandleCallbackAsync(input, CancellationToken.None);

            return Results.Redirect(result.ResumeAuthorizeUrl);
        });
    }

    private static bool TryParseProvider(string value, out ExternalProviderTypes provider)
    {
        return Enum.TryParse(value, true, out provider);
    }
}

