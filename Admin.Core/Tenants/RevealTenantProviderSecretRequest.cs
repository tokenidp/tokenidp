namespace Admin.Core.Tenants;

public sealed class RevealTenantProviderSecretRequest
{
    public ExternalProviderTypes ProviderType { get; set; }
}

public sealed class RevealTenantProviderSecretResponse
{
    public ExternalProviderTypes ProviderType { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
}