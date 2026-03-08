namespace IDP.Domain.AggregateRoots.Clients;

public class ClientExternalProvider : Entity<int>
{
    private ClientExternalProvider() { }

    public ClientExternalProvider(int externalProviderId,
        bool enabledForClient = true)
    {
        ExternalProviderId = externalProviderId;
        EnabledForClient = enabledForClient;
    }

    public int ClientId { get; private set; }
    public int ExternalProviderId { get; private set; }

    public bool EnabledForClient { get; private set; } = true;

    public virtual Client Client { get; private set; } = default!;

    public static ClientExternalProvider Create(int externalProviderId)
        => new(externalProviderId, enabledForClient: true);

    public void Update(bool enabledForClient)
    {
        EnabledForClient = enabledForClient;
    }
}