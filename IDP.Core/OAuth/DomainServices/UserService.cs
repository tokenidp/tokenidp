using IDP.Core.Model;
using System.Security.Claims;

namespace IDP.Core;

internal sealed class UserService
{
    private readonly UserManager<User> _userManager;
    private readonly IAppLogger<UserService> _logger;

    private HashSet<string> _supportedScopes;

    public UserService(UserManager<User> userManager, IAppLogger<UserService> logger)
    {
        _supportedScopes = new HashSet<string>(DefaultScopes.DefaultSupportedScopes,
            StringComparer.Ordinal);

        _userManager = userManager;
        _logger = logger;
    }

    internal async Task<IResult> HandleAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
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

            return Results.BadRequest(scopeValidation.Description);
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

            return Results.BadRequest("Missing subject claim.");
        }

        var payload = await BuildUserInfoResponse(scopes, subject);

        return Results.Ok(payload);
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

    private void AddScopes(HashSet<string> scopes, string value)
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

    private string? GetRequiredClaim(
        ClaimsPrincipal principal,
        string claimType,
        string fallbackClaimType)
    {
        return principal.FindFirst(claimType)?.Value
            ?? principal.FindFirst(fallbackClaimType)?.Value;
    }

    private async Task<Dictionary<string, object?>> BuildUserInfoResponse(HashSet<string> scopes, string subject)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sub"] = subject
        };

        var user = await _userManager.FindByIdAsync(subject);

        if (user == null)
        {
            _logger.LogWarning("User not found with username or email: {UserId}", subject);

            throw new NotFoundException(string.Format("User not found with username or email: {UserId}", subject));
        }

        if (scopes.Contains("profile"))
        {
            payload["name"] = user.FullName;
            payload["given_name"] = user.FirstName;
            payload["family_name"] = user.LastName;
            payload["middle_name"] = user.NormalizedUserName;
            payload["preferred_username"] = user.UserName;
            payload["profile"] = string.Empty;
            payload["picture"] = string.Empty;
            payload["website"] = string.Empty;
            payload["updated_at"] = user.UpdatedOn;
        }

        if (scopes.Contains("email"))
        {
            payload["email"] = user.Email;
            payload["email_verified"] = user.EmailConfirmed;
        }

        if (scopes.Contains("phone"))
        {
            payload["phone_number"] = user.PhoneNumber;
            payload["phone_number_verified"] = user.PhoneNumberConfirmed;
        }

        return payload;
    }

    private async Task WriteErrorAsync(
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

    private string BuildAuthenticateHeader(
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
