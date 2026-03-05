using IDP.Domain.AggregateRoots.Tenants;
using IDP.ExternalProviders.Abstractions;
using IDP.ExternalProviders.Model;

namespace IDP.Core.Endpoints;

public static class ExternalAuthEndpoints
{
    public static IEndpointRouteBuilder MapExternalAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/external");

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
            ITenantContextAccessor tenantContextAccessor,
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
                tenantContextAccessor.TenantId,
                tenantContextAccessor.ClientId,
                providerType,
                code,
                state);

            var result = await useCase.HandleCallbackAsync(input, httpContext.RequestAborted);

            return Results.Redirect(result.ResumeAuthorizeUrl);
        });

        return endpoints;
    }

    private static bool TryParseProvider(string value, out ExternalProviderTypes provider)
    {
        return Enum.TryParse(value, true, out provider);
    }
}
