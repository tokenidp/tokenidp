namespace TokenIDP.Core.Admin.Tenants;

public sealed class UpdateTenantSocialProvider
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string Scopes { get; set; } = string.Empty;
}
