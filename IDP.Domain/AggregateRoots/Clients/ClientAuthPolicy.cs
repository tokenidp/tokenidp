namespace IDP.Domain.AggregateRoots.Clients;

public class ClientAuthPolicy : Entity<int>
{
    private ClientAuthPolicy() { }

    public int ClientId { get; private set; }

    public bool AllowLocalLoginOverride { get; private set; }
    public bool AllowSelfRegistrationOverride { get; private set; }
    public bool MfaPolicyOverride { get; private set; }

    public bool ShowExternalProviders { get; private set; }
    public bool ShowStaySignedIn { get; private set; }
    public bool ShowCreateAccountLink { get; private set; }

    public virtual Client Client { get; private set; } = default!;

    public static ClientAuthPolicy Create(bool allowLocalLoginOverride,
        bool allowSelfRegistrationOverride,
        bool mfaPolicyOverride,
        bool showExternalProviders,
        bool showStaySignedIn,
        bool showCreateAccountLink)
    {
        return new ClientAuthPolicy()
        {
            AllowLocalLoginOverride = allowLocalLoginOverride,
            AllowSelfRegistrationOverride = allowSelfRegistrationOverride,
            MfaPolicyOverride = mfaPolicyOverride,
            ShowExternalProviders = showExternalProviders,
            ShowStaySignedIn = showStaySignedIn,
            ShowCreateAccountLink = showCreateAccountLink
        };
    }

    public void update(bool allowLocalLoginOverride,
        bool allowSelfRegistrationOverride,
        bool mfaPolicyOverride,
        bool showExternalProviders,
        bool showStaySignedIn,
        bool showCreateAccountLink)
    {
        AllowLocalLoginOverride = allowLocalLoginOverride;
        AllowSelfRegistrationOverride = allowSelfRegistrationOverride;
        MfaPolicyOverride = mfaPolicyOverride;
        ShowExternalProviders = showExternalProviders;
        ShowStaySignedIn = showStaySignedIn;
        ShowCreateAccountLink = showCreateAccountLink;
    }
}
