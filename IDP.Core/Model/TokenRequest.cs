namespace IDP.Core.Model;

public class TokenRequest
{
    public string GrantType { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string? Code { get; init; }
    public string? CodeVerifier { get; init; }
    public string RedirectUri { get; init; } = string.Empty;

    public string? RefreshToken { get; init; }
    public string? DeviceCode { get; init; }

    public string Scope { get; init; } = string.Empty;
    public string? IpAddress { get; set; }

    public string? ClientSecret { get; set; }
    public string? ClientAuthenticationMethod { get; private set; }

    // Not bindable from client
    public int TenantId { get; private set; }

    public void SetTenantId(int tenantId)
    {
        TenantId = tenantId;
    }

    public void SetClientAuthenticationMethod(string authenticationMethod)
    {
        ClientAuthenticationMethod = authenticationMethod;
    }
}