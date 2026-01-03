using IDP.Core.Model;
using System.Security.Claims;
using System.Text.Json;

namespace IDP.Core;

internal sealed class UserService
{
    private HashSet<string> _supportedScopes;

    public UserService()
    {
        _supportedScopes = new HashSet<string>( DefaultScopes.DefaultSupportedScopes,
            StringComparer.Ordinal);
    }

    public async Task HandleAsync(HttpContext httpContext, 
        CancellationToken cancellationToken = default)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            await WriteErrorAsync(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "invalid_token",
                "Missing or invalid access token.",
                cancellationToken);
            return;
        }

        var principal = httpContext.User;

        var scopes = ExtractScopes(principal);
        var scopeValidation = ValidateScopes(scopes);
        if (!scopeValidation.IsValid)
        {
            await WriteErrorAsync(
                httpContext,
                scopeValidation.StatusCode,
                scopeValidation.Error,
                scopeValidation.Description,
                cancellationToken,
                scopeValidation.Scope);
            return;
        }

        var subject = GetRequiredClaim(principal, "sub", ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            await WriteErrorAsync(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "invalid_token",
                "Missing subject claim.",
                cancellationToken);
            return;
        }

        var payload = BuildUserInfoResponse(principal, scopes, subject);

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        await httpContext.Response.WriteAsJsonAsync(payload, cancellationToken: cancellationToken);
    }

    private HashSet<string> ExtractScopes(ClaimsPrincipal principal)
    {
        var scopes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in principal.FindAll("scope"))
        {
            AddScopes(scopes, claim.Value);
        }

        foreach (var claim in principal.FindAll("scp"))
        {
            AddScopes(scopes, claim.Value);
        }

        return scopes;
    }

    private static void AddScopes(HashSet<string> scopes, string value)
    {
        foreach (var scope in value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            scopes.Add(scope);
        }
    }

    private ScopeValidationResult ValidateScopes(HashSet<string> scopes)
    {
        if (scopes.Count == 0)
        {
            return ScopeValidationResult.Insufficient(
                "openid",
                "The access token does not include any scopes.");
        }

        if (!scopes.Contains("openid"))
        {
            return ScopeValidationResult.Insufficient(
                "openid",
                "The access token must include the 'openid' scope.");
        }

        var invalidScopes = scopes
            .Where(scope => !_supportedScopes.Contains(scope))
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            return ScopeValidationResult.Invalid(
                $"Unsupported scope(s): {string.Join(" ", invalidScopes)}.");
        }

        return ScopeValidationResult.Valid();
    }

    private static string? GetRequiredClaim(
        ClaimsPrincipal principal,
        string claimType,
        string fallbackClaimType)
    {
        return principal.FindFirst(claimType)?.Value
            ?? principal.FindFirst(fallbackClaimType)?.Value;
    }

    private static Dictionary<string, object?> BuildUserInfoResponse(
        ClaimsPrincipal principal,
        HashSet<string> scopes,
        string subject)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sub"] = subject
        };

        if (scopes.Contains("profile"))
        {
            AddStringClaim(payload, principal, "name", "name");
            AddStringClaim(payload, principal, "given_name", "given_name");
            AddStringClaim(payload, principal, "family_name", "family_name");
            AddStringClaim(payload, principal, "middle_name", "middle_name");
            AddStringClaim(payload, principal, "nickname", "nickname");
            AddStringClaim(payload, principal, "preferred_username", "preferred_username");
            AddStringClaim(payload, principal, "profile", "profile");
            AddStringClaim(payload, principal, "picture", "picture");
            AddStringClaim(payload, principal, "website", "website");
            AddDateClaim(payload, principal, "updated_at", "updated_at");
        }

        if (scopes.Contains("email"))
        {
            AddStringClaim(payload, principal, "email", "email");
            AddBooleanClaim(payload, principal, "email_verified", "email_verified");
        }

        if (scopes.Contains("phone"))
        {
            AddStringClaim(payload, principal, "phone_number", "phone_number");
            AddBooleanClaim(payload, principal, "phone_number_verified", "phone_number_verified");
        }

        return payload;
    }

    private static void AddStringClaim(
        IDictionary<string, object?> payload,
        ClaimsPrincipal principal,
        string claimType,
        string fieldName)
    {
        var value = principal.FindFirst(claimType)?.Value;
        if (!string.IsNullOrWhiteSpace(value))
        {
            payload[fieldName] = value;
        }
    }

    private static void AddBooleanClaim(
        IDictionary<string, object?> payload,
        ClaimsPrincipal principal,
        string claimType,
        string fieldName)
    {
        var value = principal.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (bool.TryParse(value, out var parsed))
        {
            payload[fieldName] = parsed;
        }
        else
        {
            payload[fieldName] = value;
        }
    }

    private static void AddDateClaim(
        IDictionary<string, object?> payload,
        ClaimsPrincipal principal,
        string claimType,
        string fieldName)
    {
        var value = principal.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (long.TryParse(value, out var epoch))
        {
            payload[fieldName] = epoch;
            return;
        }

        if (DateTimeOffset.TryParse(value, out var date))
        {
            payload[fieldName] = date.ToUnixTimeSeconds();
            return;
        }

        payload[fieldName] = value;
    }

    private static void AddAddressClaim(
        IDictionary<string, object?> payload,
        ClaimsPrincipal principal,
        string claimType,
        string fieldName)
    {
        var value = principal.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (TryParseJson(value, out var element))
        {
            payload[fieldName] = element;
            return;
        }

        payload[fieldName] = value;
    }

    private static bool TryParseJson(string value, out JsonElement element)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            element = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            element = default;
            return false;
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext httpContext,
        int statusCode,
        string error,
        string description,
        CancellationToken cancellationToken,
        string? scope = null)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.Append(
            "WWW-Authenticate",
            BuildAuthenticateHeader(error, description, scope));

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                error,
                error_description = description,
                scope
            },
            cancellationToken: cancellationToken);
    }

    private static string BuildAuthenticateHeader(
        string error,
        string description,
        string? scope)
    {
        var builder = new System.Text.StringBuilder("Bearer ");
        builder.Append("error=\"").Append(error).Append('"');
        builder.Append(", error_description=\"").Append(description).Append('"');
        if (!string.IsNullOrWhiteSpace(scope))
        {
            builder.Append(", scope=\"").Append(scope).Append('"');
        }

        return builder.ToString();
    }

    private sealed record ScopeValidationResult(
        bool IsValid,
        int StatusCode,
        string Error,
        string Description,
        string? Scope)
    {
        public static ScopeValidationResult Valid()
        {
            return new ScopeValidationResult(true, StatusCodes.Status200OK, "", "", null);
        }

        public static ScopeValidationResult Invalid(string description)
        {
            return new ScopeValidationResult(
                false,
                StatusCodes.Status400BadRequest,
                "invalid_scope",
                description,
                null);
        }

        public static ScopeValidationResult Insufficient(string scope, string description)
        {
            return new ScopeValidationResult(
                false,
                StatusCodes.Status403Forbidden,
                "insufficient_scope",
                description,
                scope);
        }
    }
}
