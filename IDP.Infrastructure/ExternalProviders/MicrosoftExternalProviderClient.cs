using IDP.ExternalProviders.Model;
using IDP.ExternalProviders.Security;
using System.Text.Json.Serialization;

namespace IDP.Infrastructure.ExternalProviders;

internal sealed class MicrosoftExternalProviderClient : ExternalProviderClientBase
{
    private static readonly Uri Authority = new("https://login.microsoftonline.com/common");
    private const string Scope = "openid profile email offline_access";

    public MicrosoftExternalProviderClient(
        IHttpClientFactory httpClientFactory,
        ExternalProviderConfigurationResolver configurationResolver,
        ITenantContextAccessor tenantContextAccessor,
        ISecretProtector secretProtector)
        : base(httpClientFactory, configurationResolver, tenantContextAccessor, secretProtector)
    {
    }

    public override ExternalProviderTypes Provider => ExternalProviderTypes.Microsoft;

    public override string BuildAuthorizeUrl(ExternalChallengeRequest request)
    {
        var config = ResolveConfigurationAsync(request.TenantId).GetAwaiter().GetResult();
        var endpoint = Combine(Authority, "/oauth2/v2.0/authorize");

        var parameters = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = request.CallbackUrl,
            ["scope"] = Scope,
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
        var endpoint = Combine(Authority, "/oauth2/v2.0/token");

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("client_id", config.ClientId),
            new("client_secret", config.ClientSecret),
            new("code", request.Code),
            new("redirect_uri", request.CallbackUrl),
            new("scope", Scope)
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
        CancellationToken cancellationToken)
    {
        using var client = CreateClient(tokens.AccessToken);
        using var response = await client.GetAsync("https://graph.microsoft.com/oidc/userinfo", cancellationToken);
        var userInfo = await ReadRequiredJsonAsync<MicrosoftUserInfoResponse>(response, cancellationToken);

        var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sub"] = userInfo.Subject
        };

        if (!string.IsNullOrWhiteSpace(userInfo.Email))
        {
            claims["email"] = userInfo.Email;
        }

        if (!string.IsNullOrWhiteSpace(userInfo.Name))
        {
            claims["name"] = userInfo.Name;
        }

        return new ExternalIdentity(
            Provider,
            userInfo.Subject,
            userInfo.Email,
            userInfo.Name,
            userInfo.EmailVerified,
            claims);
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
        public bool EmailVerified { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}