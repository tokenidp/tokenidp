namespace TokenIDP.Core.Admin.Clients;

public sealed class RotateClientSecretRequest
{
    public int? ClientSecretExpiry { get; set; }
}

public sealed class RotateClientSecretResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public int? ClientSecretExpiry { get; set; }
}
