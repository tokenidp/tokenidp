using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.OAuth.UseCases;

public sealed class AuthorizationRequestValidator : IAuthorizationRequestValidator
{
    private readonly IClientStore _clientStore;

    public AuthorizationRequestValidator(IClientStore clientStore)
    {
        _clientStore = clientStore;
    }

    public async Task<ClientShortInfo> ValidateAsync(
        AuthorizationRequest request,
        CancellationToken ct)
    {
        var client = await _clientStore.GetClientShortInfo(request.ClientId);

        if (client == null || !client.IsValidClient)
        {
            throw new AuthorizationRequestException(
                error: "unauthorized_client",
                description: "Invalid client_id.",
                allowRedirect: false);
        }

        var redirectUri = request.RedirectUri?.Trim();

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Missing redirect_uri.");
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedRedirectUri))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Invalid redirect_uri.");
        }

        if (!string.Equals(parsedRedirectUri.Scheme, 
            Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !parsedRedirectUri.IsLoopback)
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "redirect_uri must use HTTPS.");
        }

        if (!string.Equals(redirectUri, client.RedirectUri?.Trim(), StringComparison.Ordinal))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Invalid redirect_uri.");
        }

        if (!string.Equals(request.ResponseType, "code",
                StringComparison.Ordinal))
        {
            throw new AuthorizationRequestException(
                "unsupported_response_type",
                "response_type must be 'code'.");
        }

        if (string.IsNullOrWhiteSpace(request.Scopes))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Missing scope.");
        }

        var allowedScopes = client.Scopes
            .ToHashSet(StringComparer.Ordinal);

        var requestedScopes = request.Scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        var invalidScopes = requestedScopes
            .Except(allowedScopes)
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            throw new AuthorizationRequestException(
                "invalid_scope",
                $"Invalid scope: {string.Join(" ", invalidScopes)}.");
        }

        if (!requestedScopes.Contains("openid"))
        {
            throw new AuthorizationRequestException(
                "invalid_scope",
                "Missing 'openid' scope.");
        }

        return client;
    }

    public async Task<ClientShortInfo> ValidateAsync(
        DeviceAuthorizationRequest request,
        CancellationToken ct)
    {
        var client = await _clientStore.GetClientShortInfo(request.ClientId);

        if (client == null || !client.IsValidClient)
        {
            throw new AuthorizationRequestException(
                error: "unauthorized_client",
                description: "Invalid client_id.",
                allowRedirect: false);
        }

        if (string.IsNullOrWhiteSpace(request.Scope))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Missing scope.");
        }

        var allowedScopes = client.Scopes
            .ToHashSet(StringComparer.Ordinal);

        var requestedScopes = request.Scope
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        var invalidScopes = requestedScopes
            .Except(allowedScopes)
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            throw new AuthorizationRequestException(
                "invalid_scope",
                $"Invalid scope: {string.Join(" ", invalidScopes)}.");
        }

        return client;
    }
}


