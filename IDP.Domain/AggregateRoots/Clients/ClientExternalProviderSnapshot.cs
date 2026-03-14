using IDP.Domain.AggregateRoots.Tenants;

namespace IDP.Domain.AggregateRoots.Clients;

public sealed class ClientExternalProviderSnapshot
{
    public ClientExternalProviderSnapshot(
        string providerType,
        bool enabledForClient,
        bool enabledForTenant,
        string clientId,
        string? clientSecret)
    {
        ProviderType = providerType;
        EnabledForClient = enabledForClient;
        EnabledForTenant = enabledForTenant;
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public string ProviderType { get; private set; }
    public bool EnabledForClient { get; private set; }
    public bool EnabledForTenant { get; private set; }
    public bool IsEnabled => EnabledForClient && EnabledForTenant;
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }
}