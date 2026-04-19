namespace TokenIDP.Domain.AggregateRoots.Clients;

public sealed class ClientExternalProviderSnapshot
{
    public ClientExternalProviderSnapshot(
        string providerType,
        bool enabledForClient,
        bool enabledForTenant,
        string clientId,
        string? clientSecret,
        string? scopes)
    {
        ProviderType = providerType;
        EnabledForClient = enabledForClient;
        EnabledForTenant = enabledForTenant;
        ClientId = clientId;
        ClientSecret = clientSecret;
        Scopes = scopes;
    }

    public string ProviderType { get; private set; }
    public bool EnabledForClient { get; private set; }
    public bool EnabledForTenant { get; private set; }
    public bool IsEnabled => EnabledForClient && EnabledForTenant;
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }
    public string? Scopes { get; init; }
}
