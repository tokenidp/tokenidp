using TokenIDP.Core.OAuth.ExternalProviders.Model;
using TokenIDP.Core.OAuth.ExternalProviders.Security;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Infrastructure.ExternalProviders;

internal sealed class GitHubExternalProviderClient : ExternalProviderClientBase
{
    private static readonly Uri Authority = new("https://github.com");
    private const string Scope = "read:user user:email";

    public GitHubExternalProviderClient(
        IHttpClientFactory httpClientFactory,
        ExternalProviderConfigurationResolver configurationResolver,
        ITenantContextAccessor tenantContextAccessor,
        ISecretProtector secretProtector)
        : base(httpClientFactory, configurationResolver, tenantContextAccessor, secretProtector)
    {
    }

    public override ExternalProviderTypes Provider => ExternalProviderTypes.GitHub;

    public override string BuildAuthorizeUrl(ExternalChallengeRequest request)
    {
        var config = ResolveConfigurationAsync(request.TenantId).GetAwaiter().GetResult();
        var endpoint = Combine(Authority, "/login/oauth/authorize");

        var parameters = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = request.CallbackUrl,
            ["scope"] = Scope,
            ["state"] = request.State
        };

        return OAuthUrlBuilder.BuildAuthorizeUrl(endpoint, parameters);
    }

    public override async Task<ExternalProviderTokens> ExchangeCodeAsync(
        ExternalCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var config = await ResolveConfigurationAsync(request.TenantId, cancellationToken);
        var endpoint = Combine(Authority, "/login/oauth/access_token");

        using var client = CreateClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.PostAsync(
            endpoint,
            new FormUrlEncodedContent(
            [
                new("client_id", config.ClientId),
                new("client_secret", config.ClientSecret),
                new("code", request.Code),
                new("redirect_uri", request.CallbackUrl),
                new("state", request.State),
                new("scope", Scope)
            ]),
            cancellationToken);

        var token = await ReadRequiredJsonAsync<GitHubTokenResponse>(response, cancellationToken);

        return new ExternalProviderTokens(
            token.AccessToken,
            null,
            null,
            3600,
            token.TokenType);
    }

    public override async Task<ExternalIdentity> GetIdentityAsync(
        ExternalProviderTokens tokens,
        ExternalCallbackRequest request,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient(tokens.AccessToken);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IDP.ExternalAuth");
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var profileResponse = await client.GetAsync("https://api.github.com/user", cancellationToken);
        var userInfo = await ReadRequiredJsonAsync<GitHubUserResponse>(profileResponse, cancellationToken);

        using var emailResponse = await client.GetAsync("https://api.github.com/user/emails", cancellationToken);
        var emails = await ReadRequiredJsonAsync<List<GitHubEmailResponse>>(emailResponse, cancellationToken);

        var primaryEmail = emails.FirstOrDefault(x => x.Primary) ?? emails.FirstOrDefault();
        var email = primaryEmail?.Email;
        var emailVerified = primaryEmail?.Verified ?? false;

        var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = userInfo.Id.ToString()
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims["email"] = email;
        }

        if (!string.IsNullOrWhiteSpace(userInfo.Name))
        {
            claims["name"] = userInfo.Name;
        }

        return new ExternalIdentity(
            Provider,
            userInfo.Id.ToString(),
            email,
            userInfo.Name ?? userInfo.Login,
            emailVerified,
            claims);
    }

    private static string Combine(Uri authority, string relativePath)
    {
        return new Uri(authority, relativePath).ToString();
    }

    private sealed class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "bearer";
    }

    private sealed class GitHubUserResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class GitHubEmailResponse
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
        [JsonPropertyName("primary")]
        public bool Primary { get; set; }
    }
}

