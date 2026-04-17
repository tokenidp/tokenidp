using TokenIDP.Domain.AggregateRoots.Clients;
using System.Text;
using System.Text.Json;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.Endpoints;

internal sealed class TokenEndpointClientAuthService
{
    private readonly IClientRepository _clientStore;
    private readonly IAppLogger<TokenEndpointClientAuthService> _logger;

    public TokenEndpointClientAuthService(
        IClientRepository clientStore,
        IAppLogger<TokenEndpointClientAuthService> logger)
    {
        _clientStore = clientStore;
        _logger = logger;
    }

    public async Task<TokenRequest> BuildValidatedRequestAsync(HttpContext httpContext)
    {
        var payload = await ParseRequestAsync(httpContext.Request);
        var clientAuthentication = ResolveClientAuthentication(httpContext.Request, payload);

        var tokenRequest = payload.ToTokenRequest(
            clientAuthentication.ClientId,
            clientAuthentication.ClientSecret,
            clientAuthentication.Method);

        ValidateRequiredFields(tokenRequest);

        await ValidateClientAuthenticationAsync(tokenRequest);

        return tokenRequest;
    }

    private async Task<ParsedTokenRequest> ParseRequestAsync(HttpRequest request)
    {
        request.EnableBuffering();

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();

            foreach (var field in form)
            {
                values[field.Key] = field.Value.ToString();
            }

            ResetRequestBody(request);

            return ParsedTokenRequest.From(values);
        }

        if (request.ContentLength is null or 0)
            return ParsedTokenRequest.From(values);

        try
        {
            var payload = await request.ReadFromJsonAsync<JsonElement>();

            if (payload is JsonElement jsonPayload && jsonPayload.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonPayload.EnumerateObject())
                {
                    values[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.Null => null,
                        JsonValueKind.String => property.Value.GetString(),
                        _ => property.Value.ToString()
                    };
                }
            }
        }
        catch (JsonException exception)
        {
            _logger.LogError("Token request body could not be parsed. Error={Error}", exception.Message);

            throw new TokenRequestValidationException("invalid_request", "Token request body is invalid.");
        }
        catch (NotSupportedException exception)
        {
            _logger.LogError("Token request content type is not supported. Error={Error}", exception.Message);

            throw new TokenRequestValidationException("invalid_request", "Token request body is invalid.");
        }

        ResetRequestBody(request);

        return ParsedTokenRequest.From(values);
    }

    private ClientAuthentication ResolveClientAuthentication(
        HttpRequest request,
        ParsedTokenRequest payload)
    {
        if (TryReadBasicCredentials(request, out var basicCredentials, out var basicError))
        {
            if (!string.IsNullOrWhiteSpace(payload.ClientSecret))
            {
                throw new TokenRequestValidationException(
                    "invalid_request",
                    "Only one client authentication method can be used per request.");
            }

            if (!string.IsNullOrWhiteSpace(payload.ClientId) &&
                !string.Equals(payload.ClientId, basicCredentials!.ClientId, StringComparison.Ordinal))
            {
                throw new TokenRequestValidationException(
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
            throw new TokenRequestValidationException("invalid_client", basicError);
        }

        return new ClientAuthentication(
            payload.ClientId,
            payload.ClientSecret,
            string.IsNullOrWhiteSpace(payload.ClientSecret)
                ? TokenEndpointAuthenticationMethods.None
                : TokenEndpointAuthenticationMethods.ClientSecretPost);
    }

    private async Task ValidateClientAuthenticationAsync(TokenRequest request)
    {
        ClientValidationSnapshot client;

        try
        {
            client = await _clientStore.GetActiveByClientId(request.ClientId);
        }
        catch (NotFoundException exception)
        {
            _logger.LogError(
                "Client authentication failed. ClientId={ClientId}, Error={Error}",
                request.ClientId,
                exception.Message);

            throw new TokenRequestValidationException("invalid_client", "Client authentication failed.");
        }

        var requiresClientSecret = IsConfidentialClient(client.ClientType);
        var usesNoClientAuthentication = string.Equals(
            request.ClientAuthenticationMethod,
            TokenEndpointAuthenticationMethods.None,
            StringComparison.Ordinal);

        if (string.Equals(request.GrantType, "client_credentials", StringComparison.Ordinal) &&
            usesNoClientAuthentication)
        {
            throw new TokenRequestValidationException("invalid_client", "Client authentication is required.");
        }

        if (string.Equals(request.GrantType, TokenGrantTypeNames.Ciba, StringComparison.OrdinalIgnoreCase) &&
            usesNoClientAuthentication)
        {
            throw new TokenRequestValidationException("invalid_client", "Client authentication is required.");
        }

        // Keep existing PKCE/browser flows working: if no secret was supplied,
        // allow the request unless the grant itself requires client auth.
        if (usesNoClientAuthentication)
            return;

        if (!requiresClientSecret)
        {
            throw new TokenRequestValidationException("invalid_client", "Client authentication is required.");
        }

        if (!ClientSecretValidator.Matches(request.ClientSecret, client.ActiveSecretHashes))
        {
            throw new TokenRequestValidationException("invalid_client", "Client secret is invalid.");
        }
    }

    private static void ValidateRequiredFields(TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GrantType))
            throw new TokenRequestValidationException("invalid_request", "grant_type is required.");

        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new TokenRequestValidationException("invalid_request", "client_id is required.");
    }

    private static bool IsConfidentialClient(ClientTypes clientType)
    {
        return clientType is ClientTypes.WebApp or ClientTypes.Backend;
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

    private static void ResetRequestBody(HttpRequest request)
    {
        if (request.Body.CanSeek)
            request.Body.Position = 0;
    }

    private sealed record ParsedTokenRequest(
        string GrantType,
        string ClientId,
        string? ClientSecret,
        string UserName,
        string Password,
        string? Code,
        string? CodeVerifier,
        string RedirectUri,
        string? RefreshToken,
        string? DeviceCode,
        string? AuthReqId,
        string Scope)
    {
        public static ParsedTokenRequest From(IReadOnlyDictionary<string, string?> values)
        {
            return new ParsedTokenRequest(
                GetValue(values, "grant_type", "grantType"),
                GetValue(values, "client_id", "clientId"),
                TrimToNull(GetValue(values, "client_secret", "clientSecret")),
                GetValue(values, "username", "userName"),
                GetValue(values, "password"),
                TrimToNull(GetValue(values, "code")),
                TrimToNull(GetValue(values, "code_verifier", "codeVerifier")),
                GetValue(values, "redirect_uri", "redirectUri"),
                TrimToNull(GetValue(values, "refresh_token", "refreshToken")),
                TrimToNull(GetValue(values, "device_code", "deviceCode")),
                TrimToNull(GetValue(values, "auth_req_id", "authReqId")),
                GetValue(values, "scope"));
        }

        public TokenRequest ToTokenRequest(string clientId, string? clientSecret, string authenticationMethod)
        {
            var request = new TokenRequest
            {
                GrantType = GrantType,
                ClientId = clientId,
                ClientSecret = clientSecret,
                UserName = UserName,
                Password = Password,
                Code = Code,
                CodeVerifier = CodeVerifier,
                RedirectUri = RedirectUri,
                RefreshToken = RefreshToken,
                DeviceCode = DeviceCode,
                AuthReqId = AuthReqId,
                Scope = Scope
            };

            request.SetClientAuthenticationMethod(authenticationMethod);

            return request;
        }

        private static string GetValue(IReadOnlyDictionary<string, string?> values, params string[] keys)
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
    }

    private sealed record BasicCredentials(string ClientId, string ClientSecret);
    private sealed record ClientAuthentication(string ClientId, string? ClientSecret, string Method);
}

internal static class TokenEndpointAuthenticationMethods
{
    public const string ClientSecretBasic = "client_secret_basic";
    public const string ClientSecretPost = "client_secret_post";
    public const string None = "none";
}

