namespace IDP.Domain.AggregateRoots.Clients;

public class ClientExternalProviderSnapShort
{
    public ClientExternalProviderSnapShort(string providerType,
        bool enabledForClient,
        string clientId, string? clientSecret,
        Uri authority,
        string callbackPath)
    {
        ProviderType = providerType;
        EnabledForClient = enabledForClient;
        ClientId = clientId;
        ClientSecret = clientSecret;
        Authority = authority;
        CallbackPath = callbackPath;
    }

    public string ProviderType { get; private set; } = default!;
    public bool EnabledForClient { get; private set; } = true;
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }
    public Uri Authority { get; init; } = default!;
    public string CallbackPath { get; init; } = default!;
}
