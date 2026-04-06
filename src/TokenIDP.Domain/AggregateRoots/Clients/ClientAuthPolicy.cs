namespace TokenIDP.Domain.AggregateRoots.Clients;

public class ClientAuthPolicy : Entity<int>
{
    private ClientAuthPolicy() { }

    public int ClientId { get; private set; }

    public bool AllowLocalLoginOverride { get; private set; }
    public bool AllowSelfRegistrationOverride { get; private set; }
    public bool MfaPolicyOverride { get; private set; }
    public bool AllowForgotPassword { get; private set; }

    public bool ShowExternalProviders { get; private set; }
    public bool ShowStaySignedIn { get; private set; }
    public bool ShowCreateAccountLink { get; private set; }
    public bool AutoCreateUsers { get; private set; } = true;
    public int? DefaultRoleId { get; private set; }

    public virtual Client Client { get; private set; } = default!;

    public static ClientAuthPolicy Create(Client client,
        bool allowLocalLoginOverride,
        bool allowSelfRegistrationOverride,
        bool mfaPolicyOverride,
        bool showExternalProviders,
        bool showStaySignedIn,
        bool showCreateAccountLink,
        bool autoCreateUsers,
        int? defaultRoleId)
    {
        if (client == null)
        {
            throw new DomainException("Client is required.");
        }

        return new ClientAuthPolicy()
        {
            Client = client,
            ClientId = client.Id,
            AllowLocalLoginOverride = allowLocalLoginOverride,
            AllowSelfRegistrationOverride = allowSelfRegistrationOverride,
            MfaPolicyOverride = mfaPolicyOverride,
            ShowExternalProviders = showExternalProviders,
            ShowStaySignedIn = showStaySignedIn,
            ShowCreateAccountLink = showCreateAccountLink,
            AutoCreateUsers = autoCreateUsers,
            DefaultRoleId = defaultRoleId
        };
    }

    public void update(bool allowLocalLoginOverride,
        bool allowSelfRegistrationOverride,
        bool mfaPolicyOverride,
        bool showExternalProviders,
        bool showStaySignedIn,
        bool showCreateAccountLink,
        bool autoCreateUsers,
        int? defaultRoleId)
    {
        AllowLocalLoginOverride = allowLocalLoginOverride;
        AllowSelfRegistrationOverride = allowSelfRegistrationOverride;
        MfaPolicyOverride = mfaPolicyOverride;
        ShowExternalProviders = showExternalProviders;
        ShowStaySignedIn = showStaySignedIn;
        ShowCreateAccountLink = showCreateAccountLink;
        AutoCreateUsers = autoCreateUsers;
        DefaultRoleId = defaultRoleId;
    }
}
