using TokenIDP.Core.OAuth.ExternalProviders.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.Endpoints;

public class LogoutEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/logout", async (HttpContext context,
            IAppLogger<LoginEndpoint> logger,
            IUserSignInService userSignInService,
            IClientRepository clientStore) =>
        {
            logger.LogDebug("Logout requested");

            var clientId = context.Request.Query["client_id"].ToString();
            var requestedRedirectUri = context.Request.Query["post_logout_redirect_uri"].ToString();
            var redirectUri = "/login";

            await userSignInService.SignOutAsync(CancellationToken.None);

            logger.LogDebug("SSO session cleared");

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                try
                {
                    var client = await clientStore.GetActiveByClientId(clientId);
                    redirectUri = ResolveRedirectUri(requestedRedirectUri, client.LogoutRedirectUri) ?? redirectUri;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Logout requested for unknown or invalid client {ClientId}. Falling back to {RedirectUri}",
                        clientId, redirectUri);
                    logger.LogDebug("Logout client resolution failed: {Message}", ex.Message);
                }
            }

            redirectUri = AppendLoggedOutFlag(redirectUri);

            logger.LogInfo("Logout completed for client {ClientId}. Redirecting to {RedirectUri}", clientId ?? string.Empty, redirectUri);
            return Results.Redirect(redirectUri);
        })
        .AllowAnonymous()
        .WithName("Logout")
        .WithTags("Logout");
    }

    private static string? ResolveRedirectUri(string? requestedRedirectUri, string? configuredRedirectUris)
    {
        var allowedRedirectUris = ParseRedirectUris(configuredRedirectUris);

        if (!string.IsNullOrWhiteSpace(requestedRedirectUri) &&
            allowedRedirectUris.Contains(requestedRedirectUri, StringComparer.OrdinalIgnoreCase))
        {
            return requestedRedirectUri;
        }

        return allowedRedirectUris.FirstOrDefault();
    }

    private static IReadOnlyList<string> ParseRedirectUris(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static uri => !string.IsNullOrWhiteSpace(uri))
                .ToArray();

    private static string AppendLoggedOutFlag(string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return redirectUri;
        }

        return QueryHelpers.AddQueryString(redirectUri, "logged_out", "1");
    }
}

