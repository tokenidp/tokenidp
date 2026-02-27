using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

public sealed class AuthorizationRequestValidator
    : IAuthorizationRequestValidator
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

        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Missing redirect_uri.");
        }

        if (!Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out _))
        {
            throw new AuthorizationRequestException(
                "invalid_request",
                "Invalid redirect_uri.");
        }

        if (!string.Equals(
                request.ResponseType,
                "code",
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
}

