using TokenIDP.Core.OAuth.ExternalProviders.Model;
using TokenIDP.Core.OAuth.ExternalProviders.Security;
using System.Text.Json.Serialization;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Infrastructure.ExternalProviders;

internal sealed class MicrosoftExternalProviderClient : ExternalProviderClientBase
{
    private static readonly Uri Authority = new("https://login.microsoftonline.com/common");
    private const string Scope = "openid profile email offline_access";
    private readonly OidcIdTokenValidator _idTokenValidator;

    public MicrosoftExternalProviderClient(
        IHttpClientFactory httpClientFactory,
        ExternalProviderConfigurationResolver configurationResolver,
        ITenantContextAccessor tenantContextAccessor,
        ISecretProtector secretProtector,
        OidcIdTokenValidator idTokenValidator)
        : base(httpClientFactory, configurationResolver, tenantContextAccessor, secretProtector)
    {
        _idTokenValidator = idTokenValidator;
    }

    public override ExternalProviderTypes Provider => ExternalProviderTypes.Microsoft;

    public override string BuildAuthorizeUrl(ExternalChallengeRequest request)
    {
        var config = ResolveConfigurationAsync(request.TenantId).GetAwaiter().GetResult();
        var scope = string.IsNullOrWhiteSpace(config.Scopes) ? Scope : config.Scopes;
        var endpoint = Combine(Authority, "/oauth2/v2.0/authorize");

        var parameters = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = request.CallbackUrl,
            ["scope"] = scope,
            ["state"] = request.State,
            ["nonce"] = request.Nonce,
            ["code_challenge"] = request.CodeVerifier is null ? null : PkceGenerator.CreateCodeChallenge(request.CodeVerifier),
            ["code_challenge_method"] = request.CodeVerifier is null ? null : "S256"
        };

        return OAuthUrlBuilder.BuildAuthorizeUrl(endpoint, parameters);
    }

    public override async Task<ExternalProviderTokens> ExchangeCodeAsync(
        ExternalCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var config = await ResolveConfigurationAsync(request.TenantId, cancellationToken);
        var scope = string.IsNullOrWhiteSpace(config.Scopes) ? Scope : config.Scopes;
        var endpoint = Combine(Authority, "/oauth2/v2.0/token");

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("client_id", config.ClientId),
            new("client_secret", config.ClientSecret),
            new("code", request.Code),
            new("redirect_uri", request.CallbackUrl),
            new("scope", scope)
        };

        if (!string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            form.Add(new("code_verifier", request.CodeVerifier));
        }

        using var client = CreateClient();
        using var response = await client.PostAsync(
            endpoint,
            new FormUrlEncodedContent(form),
            cancellationToken);

        var token = await ReadRequiredJsonAsync<MicrosoftTokenResponse>(response, cancellationToken);

        return new ExternalProviderTokens(
            token.AccessToken,
            token.RefreshToken,
            token.IdToken,
            token.ExpiresIn,
            token.TokenType);
    }

    public override async Task<ExternalIdentity> GetIdentityAsync(
        ExternalProviderTokens tokens,
        ExternalCallbackRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokens.IdToken))
        {
            throw new InvalidOperationException("Microsoft did not return an id_token.");
        }

        var config = await ResolveConfigurationAsync(request.TenantId, cancellationToken);
        var validatedToken = await _idTokenValidator.ValidateMicrosoftAsync(
            config.ClientId,
            tokens.IdToken,
            request.Nonce,
            cancellationToken);

        using var client = CreateClient(tokens.AccessToken);
        using var response = await client.GetAsync("https://graph.microsoft.com/oidc/userinfo", cancellationToken);
        var userInfo = await ReadRequiredJsonAsync<MicrosoftUserInfoResponse>(response, cancellationToken);

        if (!string.Equals(userInfo.Subject, validatedToken.Subject, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Microsoft userinfo subject did not match the validated id_token.");
        }

        if (!string.IsNullOrWhiteSpace(validatedToken.Email) &&
            !string.IsNullOrWhiteSpace(userInfo.Email) &&
            !string.Equals(validatedToken.Email, userInfo.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Microsoft userinfo email did not match the validated id_token.");
        }

        var email = validatedToken.Email ?? userInfo.Email;
        var displayName = validatedToken.Name ?? userInfo.Name;
        var emailVerified = ResolveEmailVerified(
            email,
            validatedToken.Email,
            validatedToken.EmailVerified,
            userInfo.Email,
            userInfo.EmailVerified);

        var claims = new Dictionary<string, string>(validatedToken.Claims, StringComparer.OrdinalIgnoreCase)
        {
            ["sub"] = validatedToken.Subject
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims["email"] = email;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            claims["name"] = displayName;
        }

        return new ExternalIdentity(
            Provider,
            validatedToken.Subject,
            email,
            displayName,
            emailVerified,
            claims);
    }

    private static bool ResolveEmailVerified(
        string? resolvedEmail,
        string? tokenEmail,
        bool? tokenEmailVerified,
        string? userInfoEmail,
        bool? userInfoEmailVerified)
    {
        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            return false;
        }

        var verified = false;

        if (string.Equals(resolvedEmail, tokenEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (tokenEmailVerified is false)
            {
                return false;
            }

            verified |= tokenEmailVerified is true;
        }

        if (string.Equals(resolvedEmail, userInfoEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (userInfoEmailVerified is false)
            {
                return false;
            }

            verified |= userInfoEmailVerified is true;
        }

        return verified;
    }

    private static string Combine(Uri authority, string relativePath)
    {
        return new Uri(authority, relativePath).ToString();
    }

    private sealed class MicrosoftTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";
    }

    private sealed class MicrosoftUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("email_verified")]
        public bool? EmailVerified { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}

