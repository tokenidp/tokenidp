namespace IDP.Domain.AggregateRoots.Clients;

public class ClientExternalProvider : Entity<int>
{
    private ClientExternalProvider() { }

    public ClientExternalProvider(int externalProviderId,
        bool enabledForClient = true,
        bool autoCreateUsers = true,
        int? defaultRoleId = null)
    {
        ExternalProviderId = externalProviderId;
        EnabledForClient = enabledForClient;
        AutoCreateUsers = autoCreateUsers;
        DefaultRoleId = defaultRoleId;
    }

    public int ClientId { get; private set; }
    public int ExternalProviderId { get; private set; }

    public bool EnabledForClient { get; private set; } = true;
    public bool AutoCreateUsers { get; private set; } = true;
    public int? DefaultRoleId { get; private set; }

    public virtual Client Client { get; private set; } = default!;

    public static ClientExternalProvider Create(
        int externalProviderId,
        bool autoCreateUsers = true,
        int? defaultRoleId = null)
    {
        return new ClientExternalProvider(
            externalProviderId,
            enabledForClient: true,
            autoCreateUsers: autoCreateUsers,
            defaultRoleId: defaultRoleId);
    }

    public void Update(bool enabledForClient)
    {
        EnabledForClient = enabledForClient;
    }

    public void UpdateProvisioningPolicy(bool autoCreateUsers, int? defaultRoleId)
    {
        AutoCreateUsers = autoCreateUsers;
        DefaultRoleId = defaultRoleId;
    }
}