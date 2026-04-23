using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;
using TokenOptions = TokenIDP.Core.Foundation.Options.TokenOptions;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class CibaUserResolver
{
    private readonly IUserRepository _userRepository;
    private readonly IHostEnvironment _environment;
    private readonly TokenOptions _tokenOptions;

    public CibaUserResolver(
        IUserRepository userRepository,
        IHostEnvironment environment,
        IOptions<TokenOptions> tokenOptions)
    {
        _userRepository = userRepository;
        _environment = environment;
        _tokenOptions = tokenOptions.Value;
    }

    public async Task<CibaResolvedUser> ResolveAsync(
        ClientValidationSnapshot client,
        CibaBackchannelAuthenticationRequest request,
        CancellationToken ct)
    {
        var providedHints = new[]
        {
            !string.IsNullOrWhiteSpace(request.LoginHint),
            !string.IsNullOrWhiteSpace(request.LoginHintToken),
            !string.IsNullOrWhiteSpace(request.IdTokenHint)
        }.Count(x => x);

        if (providedHints != 1)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_request",
                "Exactly one of login_hint, login_hint_token, or id_token_hint is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.LoginHint))
        {
            if (!client.AllowCibaLoginHint)
            {
                throw new BackchannelAuthenticationValidationException(
                    "invalid_request",
                    "login_hint is not allowed for this client.");
            }

            return await ResolveFromLoginHintAsync(
                request.TenantId,
                request.LoginHint!,
                CibaUserHintType.LoginHint,
                ct);
        }

        if (!string.IsNullOrWhiteSpace(request.LoginHintToken))
        {
            if (!client.AllowCibaLoginHintToken)
            {
                throw new BackchannelAuthenticationValidationException(
                    "invalid_request",
                    "login_hint_token is not allowed for this client.");
            }

            return await ResolveFromLoginHintTokenAsync(
                request.TenantId,
                request.LoginHintToken!,
                ct);
        }

        if (!client.AllowCibaIdTokenHint)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_request",
                "id_token_hint is not allowed for this client.");
        }

        return await ResolveFromIdTokenHintAsync(
            request.TenantId,
            client.ClientId,
            request.IdTokenHint!,
            ct);
    }

    private async Task<CibaResolvedUser> ResolveFromLoginHintAsync(
        int tenantId,
        string loginHint,
        CibaUserHintType hintType,
        CancellationToken ct)
    {
        var user = await _userRepository.FindByLoginHintAsync(tenantId, loginHint, ct);
        return CreateResolvedUser(user, hintType, loginHint);
    }

    private async Task<CibaResolvedUser> ResolveFromLoginHintTokenAsync(
        int tenantId,
        string loginHintToken,
        CancellationToken ct)
    {
        var payload = ReadJwtPayload(loginHintToken) ?? ReadJsonPayload(loginHintToken);

        if (payload is null)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_request",
                "login_hint_token could not be parsed.");
        }

        if (payload.TryGetValue("exp", out var expValue) &&
            long.TryParse(expValue, out var expUnix) &&
            DateTimeOffset.FromUnixTimeSeconds(expUnix) <= DateTimeOffset.UtcNow)
        {
            throw new BackchannelAuthenticationValidationException(
                "expired_login_hint_token",
                "The login_hint_token has expired.");
        }

        var candidateHint = FirstNonEmpty(
            GetValue(payload, "login_hint"),
            GetValue(payload, "email"),
            GetValue(payload, "preferred_username"),
            GetValue(payload, "username"),
            GetValue(payload, "sub"));

        if (string.IsNullOrWhiteSpace(candidateHint))
        {
            throw new BackchannelAuthenticationValidationException(
                "unknown_user_id",
                "The provided login_hint_token could not be resolved to a user.");
        }

        var user = await _userRepository.FindByLoginHintAsync(tenantId, candidateHint, ct);
        return CreateResolvedUser(user, CibaUserHintType.LoginHintToken, loginHintToken);
    }

    private async Task<CibaResolvedUser> ResolveFromIdTokenHintAsync(
        int tenantId,
        string clientId,
        string idTokenHint,
        CancellationToken ct)
    {
        var principal = ValidateIdTokenHint(clientId, idTokenHint);
        var userIdClaim = principal.FindFirstValue("user_id");
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var userId = TryParseUserId(userIdClaim) ?? TryParseUserId(sub);
        if (!userId.HasValue)
        {
            throw new BackchannelAuthenticationValidationException(
                "unknown_user_id",
                "The provided id_token_hint could not be resolved to a user.");
        }

        var user = await _userRepository.GetByTenantAsync(userId.Value, tenantId, ct);
        return CreateResolvedUser(user, CibaUserHintType.IdTokenHint, idTokenHint);
    }

    private CibaResolvedUser CreateResolvedUser(
        User? user,
        CibaUserHintType hintType,
        string rawHint)
    {
        if (user == null)
        {
            throw new BackchannelAuthenticationValidationException(
                "unknown_user_id",
                "The provided user hint could not be resolved.");
        }

        if (user.StatusId != UserStatus.Active || user.IsLockedOut())
        {
            throw new BackchannelAuthenticationValidationException(
                "access_denied",
                "The identified user is not allowed to authenticate.");
        }

        return new CibaResolvedUser(
            user.Id,
            user.TenantId,
            hintType,
            SecretHasher.HashSecret(rawHint),
            MaskSubjectHint(user.Email, user.UserName),
            user.UserCode);
    }

    private ClaimsPrincipal ValidateIdTokenHint(string clientId, string idTokenHint)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var issuer = TokenOptionsResolver.ResolveIssuer(_tokenOptions);
        var signingKey = ResolveSigningKey();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = false,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(idTokenHint, validationParameters, out _);
            var audiences = principal.FindAll("aud").Select(x => x.Value).ToArray();

            if (!audiences.Contains(clientId, StringComparer.Ordinal))
            {
                throw new BackchannelAuthenticationValidationException(
                    "unknown_user_id",
                    "The provided id_token_hint is not valid for this client.");
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            throw new BackchannelAuthenticationValidationException(
                "unknown_user_id",
                "The provided id_token_hint is not valid.");
        }
        catch (ArgumentException)
        {
            throw new BackchannelAuthenticationValidationException(
                "unknown_user_id",
                "The provided id_token_hint is not valid.");
        }
    }

    private SecurityKey ResolveSigningKey()
    {
        if (TokenSigningMaterialResolver.HasCertificateConfiguration(_tokenOptions))
        {
            var certificate = TokenSigningMaterialResolver.LoadCertificate(_tokenOptions);
            return new X509SecurityKey(certificate);
        }

        if (_environment.IsProduction())
        {
            throw new InvalidOperationException("Token signing material is not configured.");
        }

        var keyMaterial = TokenSigningMaterialResolver.ResolveKeyMaterial(_tokenOptions);
        var rsa = System.Security.Cryptography.RSA.Create();

        if (keyMaterial.Contains("BEGIN", StringComparison.Ordinal))
        {
            rsa.ImportFromPem(keyMaterial);
            return new RsaSecurityKey(rsa);
        }

        rsa.ImportRSAPrivateKey(Convert.FromBase64String(keyMaterial), out _);
        return new RsaSecurityKey(rsa);
    }

    private static Dictionary<string, string>? ReadJwtPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return null;
            }

            var payloadBytes = WebEncoders.Base64UrlDecode(parts[1]);
            var json = System.Text.Encoding.UTF8.GetString(payloadBytes);
            return ReadJsonPayload(json);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Dictionary<string, string>? ReadJsonPayload(string json)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                        _ => property.Value.ToString()
                    },
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> payload, string key)
    {
        return payload.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string MaskSubjectHint(string email, string userName)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var at = email.IndexOf('@');
            if (at > 1)
            {
                return $"{email[0]}***{email[(at - 1)..]}";
            }
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Length <= 2
                ? $"{userName[0]}*"
                : $"{userName[0]}***{userName[^1]}";
        }

        return string.Empty;
    }

    private static int? TryParseUserId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.StartsWith("usr:", StringComparison.OrdinalIgnoreCase)
            ? value["usr:".Length..]
            : value;

        return int.TryParse(candidate, out var parsedValue)
            ? parsedValue
            : null;
    }

    internal sealed record CibaResolvedUser(
        int UserId,
        int TenantId,
        CibaUserHintType HintType,
        string HintValueHash,
        string SubjectHint,
        string? ExpectedUserCode);
}
