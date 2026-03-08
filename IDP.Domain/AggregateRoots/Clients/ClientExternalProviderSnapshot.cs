using IDP.Domain.AggregateRoots.Tenants;

namespace IDP.Domain.AggregateRoots.Clients;

public sealed class ClientExternalProviderSnapshot
{
    public ClientExternalProviderSnapshot(
        string providerType,
        bool enabledForClient,
        bool enabledForTenant,
        string clientId,
        string? clientSecret,
        Uri authority,
        string callbackPath,
        IReadOnlyCollection<string>? scopes)
    {
        ProviderType = providerType;
        EnabledForClient = enabledForClient;
        EnabledForTenant = enabledForTenant;
        ClientId = clientId;
        ClientSecret = clientSecret;
        Authority = authority;
        CallbackPath = callbackPath;
        Scopes = scopes ?? Array.Empty<string>();
    }

    public string ProviderType { get; private set; }
    public bool EnabledForClient { get; private set; }
    public bool EnabledForTenant { get; private set; }
    public bool IsEnabled => EnabledForClient && EnabledForTenant;
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }
    public Uri Authority { get; init; } = default!;
    public string CallbackPath { get; init; } = default!;
    public IReadOnlyCollection<string> Scopes { get; init; } = Array.Empty<string>();
}