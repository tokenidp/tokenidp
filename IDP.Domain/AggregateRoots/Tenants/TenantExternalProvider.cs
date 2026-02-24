namespace IDP.Domain.AggregateRoots.Tenants;

public class TenantExternalProvider : Entity<int>
{
    private TenantExternalProvider() { } // EF

    public int TenantId { get; private set; }
    public ExternalProviderTypes ProviderType { get; private set; }
    public bool Enabled { get; private set; }

    public OidcClientConfig? OidcConfig { get; private set; }

    public virtual Tenant Tenant { get; private set; } = default!;

    public static TenantExternalProvider Create(
        int tenantId,
        ExternalProviderTypes providerType,
        OidcClientConfig config)
    {
        return new TenantExternalProvider
        {
            TenantId = tenantId,
            ProviderType = providerType,
            Enabled = true,
            OidcConfig = config
        };
    }

    public void Enable()
    {
        if (OidcConfig is null)
            throw new DomainException("Cannot enable provider without OIDC configuration.");

        Enabled = true;
    }

    public void Disable() => Enabled = false;

    public void UpdateOidcConfig(OidcClientConfig config)
    {
        OidcConfig = config ?? throw new DomainException("OIDC config is required.");
    }
}