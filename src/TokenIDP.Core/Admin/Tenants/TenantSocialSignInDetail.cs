namespace TokenIDP.Core.Admin.Tenants;

public sealed class TenantSocialSignInDetail
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public List<TenantSocialProviderDetail> Providers { get; set; } = new();
}

public sealed class TenantSocialProviderDetail
{
    public ExternalProviderTypes ProviderType { get; set; }
    public bool Enabled { get; set; }
    public bool HasClientSecret { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string Scopes { get; set; } = string.Empty;
}

