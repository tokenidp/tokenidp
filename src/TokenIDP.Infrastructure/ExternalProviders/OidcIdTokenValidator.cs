using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TokenIDP.Infrastructure.ExternalProviders;

internal sealed class OidcIdTokenValidator
{
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> GoogleConfigurationManager =
        CreateConfigurationManager("https://accounts.google.com/.well-known/openid-configuration");

    private static readonly ConfigurationManager<OpenIdConnectConfiguration> MicrosoftConfigurationManager =
        CreateConfigurationManager("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration");

    public async Task<ValidatedIdToken> ValidateGoogleAsync(
        string clientId,
        string idToken,
        string? expectedNonce,
        CancellationToken cancellationToken)
    {
        var configuration = await GoogleConfigurationManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuer = true,
            ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        return Validate(idToken, validationParameters, expectedNonce);
    }

    public async Task<ValidatedIdToken> ValidateMicrosoftAsync(
        string clientId,
        string idToken,
        string? expectedNonce,
        CancellationToken cancellationToken)
    {
        var configuration = await MicrosoftConfigurationManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuer = true,
            IssuerValidator = ValidateMicrosoftIssuer,
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        return Validate(idToken, validationParameters, expectedNonce);
    }

    private static ValidatedIdToken Validate(
        string idToken,
        TokenValidationParameters validationParameters,
        string? expectedNonce)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

            var principal = handler.ValidateToken(idToken, validationParameters, out _);

            var subject = FindClaimValue(principal, "sub");
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new InvalidOperationException("External provider id_token is missing the sub claim.");
            }

            if (!string.IsNullOrWhiteSpace(expectedNonce))
            {
                var actualNonce = FindClaimValue(principal, "nonce");
                if (!string.Equals(actualNonce, expectedNonce, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("External provider nonce validation failed.");
                }
            }

            var claims = principal.Claims
                .Where(static claim => !string.IsNullOrWhiteSpace(claim.Type))
                .GroupBy(static claim => claim.Type, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.First().Value,
                    StringComparer.OrdinalIgnoreCase);

            return new ValidatedIdToken(
                subject,
                FindClaimValue(principal, "email") ?? FindClaimValue(principal, "preferred_username"),
                TryReadBooleanClaim(principal, "email_verified"),
                FindClaimValue(principal, "name"),
                claims);
        }
        catch (SecurityTokenException ex)
        {
            throw new InvalidOperationException("External provider id_token validation failed.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("External provider id_token validation failed.", ex);
        }
    }

    private static string ValidateMicrosoftIssuer(
        string issuer,
        SecurityToken securityToken,
        TokenValidationParameters validationParameters)
    {
        if (securityToken is not JwtSecurityToken jwtToken)
        {
            throw new SecurityTokenInvalidIssuerException("Microsoft id_token format is invalid.");
        }

        var tenantId = jwtToken.Claims.FirstOrDefault(static claim => claim.Type == "tid")?.Value;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new SecurityTokenInvalidIssuerException("Microsoft id_token is missing the tid claim.");
        }

        var expectedIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        if (!string.Equals(issuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenInvalidIssuerException(
                $"The issuer '{issuer}' is not valid for tenant '{tenantId}'.");
        }

        return issuer;
    }

    private static bool? TryReadBooleanClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = FindClaimValue(principal, claimType);
        return bool.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? FindClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }

    private static ConfigurationManager<OpenIdConnectConfiguration> CreateConfigurationManager(string metadataAddress)
    {
        return new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever
            {
                RequireHttps = true
            });
    }
}

internal sealed record ValidatedIdToken(
    string Subject,
    string? Email,
    bool? EmailVerified,
    string? Name,
    IReadOnlyDictionary<string, string> Claims
);

