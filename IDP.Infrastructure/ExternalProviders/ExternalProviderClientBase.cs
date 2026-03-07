using IDP.ExternalProviders.Abstractions;
using IDP.ExternalProviders.Model;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IDP.Infrastructure.ExternalProviders;

internal abstract class ExternalProviderClientBase : IExternalProviderClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalProviderConfigurationResolver _configurationResolver;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ISecretProtector _secretProtector;

    public ExternalProviderClientBase(
        IHttpClientFactory httpClientFactory,
        ExternalProviderConfigurationResolver configurationResolver,
        ITenantContextAccessor tenantContextAccessor,
        ISecretProtector secretProtector)
    {
        _httpClientFactory = httpClientFactory;
        _configurationResolver = configurationResolver;
        _tenantContextAccessor = tenantContextAccessor;
        _secretProtector = secretProtector;
    }

    public abstract ExternalProviderTypes Provider { get; }

    public abstract string BuildAuthorizeUrl(ExternalChallengeRequest request);

    public abstract Task<ExternalProviderTokens> ExchangeCodeAsync(
        ExternalCallbackRequest request,
        CancellationToken cancellationToken);

    public abstract Task<ExternalIdentity> GetIdentityAsync(
        ExternalProviderTokens tokens,
        CancellationToken cancellationToken);

    protected async Task<ResolvedProviderConfiguration> ResolveConfigurationAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var clientId = _tenantContextAccessor.ClientId;
        if (clientId <= 0)
        {
            throw new InvalidOperationException("Client context is required for external authentication.");
        }

        var config = await _configurationResolver.ResolveAsync(tenantId, clientId, Provider, cancellationToken);
        if (config is null)
        {
            throw new InvalidOperationException($"{Provider} is not configured for this tenant/client.");
        }

        var secretContext = BuildSecretContext(tenantId.ToString(), Provider);
        var clientSecret = _secretProtector.Decrypt(config.ClientSecret, secretContext) ?? string.Empty;

        return new ResolvedProviderConfiguration(
            config.ClientId,
            clientSecret,
            config.Authority,
            config.CallbackPath,
            config.Scopes);
    }

    protected HttpClient CreateClient(string? bearerToken = null)
    {
        var client = _httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return client;
    }

    protected static async Task<T> ReadRequiredJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        if (model is null)
        {
            throw new InvalidOperationException("External provider returned an empty response.");
        }

        return model;
    }

    protected static string BuildSecretContext(string tenantId, ExternalProviderTypes providerType)
    {
        return $"tenant:{tenantId}:provider:{providerType}";
    }
}

internal sealed record ResolvedProviderConfiguration(
    string ClientId,
    string ClientSecret,
    Uri Authority,
    string CallbackPath,
    IReadOnlyCollection<string> Scopes
);