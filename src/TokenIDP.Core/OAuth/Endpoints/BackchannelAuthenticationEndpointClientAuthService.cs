using System.Text;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.Endpoints;

internal sealed class BackchannelAuthenticationEndpointClientAuthService
{
    private readonly IClientRepository _clientStore;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public BackchannelAuthenticationEndpointClientAuthService(
        IClientRepository clientStore,
        ITenantContextAccessor tenantContextAccessor)
    {
        _clientStore = clientStore;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<CibaBackchannelAuthenticationRequest> BuildValidatedRequestAsync(HttpContext httpContext)
    {
        var payload = await ParseRequestAsync(httpContext.Request);
        var clientAuthentication = ResolveClientAuthentication(httpContext.Request, payload);

        if (string.IsNullOrWhiteSpace(clientAuthentication.ClientId) ||
            string.IsNullOrWhiteSpace(clientAuthentication.ClientSecret))
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_client",
                "Client authentication is required.");
        }

        var client = await ValidateClientAuthenticationAsync(
            clientAuthentication.ClientId,
            clientAuthentication.ClientSecret,
            clientAuthentication.Method);

        var request = new CibaBackchannelAuthenticationRequest
        {
            Scope = payload.Scope,
            LoginHint = payload.LoginHint,
            LoginHintToken = payload.LoginHintToken,
            IdTokenHint = payload.IdTokenHint,
            BindingMessage = payload.BindingMessage,
            UserCode = payload.UserCode,
            RequestedExpiry = payload.RequestedExpiry,
            AcrValues = payload.AcrValues,
            ClientNotificationToken = payload.ClientNotificationToken
        };

        request.SetClientAuthentication(
            clientAuthentication.ClientId,
            clientAuthentication.ClientSecret,
            clientAuthentication.Method);
        request.SetTenantId(client.TenantId);

        return request;
    }

    private static async Task<ParsedBackchannelRequest> ParseRequestAsync(HttpRequest request)
    {
        if (!request.HasFormContentType)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_request",
                "Backchannel authentication requests must use application/x-www-form-urlencoded.");
        }

        var form = await request.ReadFormAsync();

        return ParsedBackchannelRequest.From(
            form.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase));
    }

    private static ClientAuthentication ResolveClientAuthentication(
        HttpRequest request,
        ParsedBackchannelRequest payload)
    {
        if (TryReadBasicCredentials(request, out var basicCredentials, out var basicError))
        {
            if (!string.IsNullOrWhiteSpace(payload.ClientSecret))
            {
                throw new BackchannelAuthenticationValidationException(
                    "invalid_request",
                    "Only one client authentication method can be used per request.");
            }

            if (!string.IsNullOrWhiteSpace(payload.ClientId) &&
                !string.Equals(payload.ClientId, basicCredentials!.ClientId, StringComparison.Ordinal))
            {
                throw new BackchannelAuthenticationValidationException(
                    "invalid_request",
                    "client_id in the request body must match the Authorization header.");
            }

            return new ClientAuthentication(
                basicCredentials!.ClientId,
                basicCredentials.ClientSecret,
                TokenEndpointAuthenticationMethods.ClientSecretBasic);
        }

        if (!string.IsNullOrWhiteSpace(basicError))
        {
            throw new BackchannelAuthenticationValidationException("invalid_client", basicError);
        }

        return new ClientAuthentication(
            payload.ClientId,
            payload.ClientSecret,
            string.IsNullOrWhiteSpace(payload.ClientSecret)
                ? TokenEndpointAuthenticationMethods.None
                : TokenEndpointAuthenticationMethods.ClientSecretPost);
    }

    private async Task<ClientValidationSnapshot> ValidateClientAuthenticationAsync(
        string clientId,
        string clientSecret,
        string authenticationMethod)
    {
        ClientValidationSnapshot client;

        try
        {
            client = await _clientStore.GetActiveByClientId(clientId);
        }
        catch (NotFoundException)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_client",
                "Client authentication failed.");
        }

        if (authenticationMethod == TokenEndpointAuthenticationMethods.None)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_client",
                "Client authentication is required.");
        }

        EnsureTenantMatch(client.TenantId);

        if (!ClientSecretValidator.Matches(clientSecret, client.ActiveSecretHashes))
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_client",
                "Client authentication failed.");
        }

        return client;
    }

    private void EnsureTenantMatch(int clientTenantId)
    {
        if (_tenantContextAccessor.HasTenant &&
            clientTenantId != _tenantContextAccessor.TenantId)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_client",
                "Client authentication failed.");
        }
    }

    private static bool TryReadBasicCredentials(
        HttpRequest request,
        out BasicCredentials? credentials,
        out string? error)
    {
        credentials = null;
        error = null;

        if (!request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            return false;

        var rawValue = authorizationHeader.ToString();

        if (!rawValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        var encodedCredentials = rawValue["Basic ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(encodedCredentials))
        {
            error = "Authorization header is invalid.";
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var separatorIndex = decoded.IndexOf(':');

            if (separatorIndex <= 0)
            {
                error = "Authorization header is invalid.";
                return false;
            }

            credentials = new BasicCredentials(
                decoded[..separatorIndex].Trim(),
                decoded[(separatorIndex + 1)..]);

            return true;
        }
        catch (FormatException)
        {
            error = "Authorization header is invalid.";
            return false;
        }
    }

    private sealed record ParsedBackchannelRequest(
        string Scope,
        string ClientId,
        string? ClientSecret,
        string? LoginHint,
        string? LoginHintToken,
        string? IdTokenHint,
        string? BindingMessage,
        string? UserCode,
        int? RequestedExpiry,
        string? AcrValues,
        string? ClientNotificationToken)
    {
        public static ParsedBackchannelRequest From(IReadOnlyDictionary<string, string> values)
        {
            return new ParsedBackchannelRequest(
                GetValue(values, "scope"),
                GetValue(values, "client_id", "clientId"),
                TrimToNull(GetValue(values, "client_secret", "clientSecret")),
                TrimToNull(GetValue(values, "login_hint", "loginHint")),
                TrimToNull(GetValue(values, "login_hint_token", "loginHintToken")),
                TrimToNull(GetValue(values, "id_token_hint", "idTokenHint")),
                TrimToNull(GetValue(values, "binding_message", "bindingMessage")),
                TrimToNull(GetValue(values, "user_code", "userCode")),
                TryGetInt(values, "requested_expiry", "requestedExpiry"),
                TrimToNull(GetValue(values, "acr_values", "acrValues")),
                TrimToNull(GetValue(values, "client_notification_token", "clientNotificationToken")));
        }

        private static string GetValue(IReadOnlyDictionary<string, string> values, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }

        private static string? TrimToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static int? TryGetInt(IReadOnlyDictionary<string, string> values, params string[] keys)
        {
            var value = GetValue(values, keys);
            return int.TryParse(value, out var parsed)
                ? parsed
                : null;
        }
    }

    private sealed record BasicCredentials(string ClientId, string ClientSecret);
    private sealed record ClientAuthentication(string ClientId, string? ClientSecret, string Method);
}
