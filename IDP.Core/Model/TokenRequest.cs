namespace IDP.Core.Model;

public class TokenRequest
{
    public required string GrantType { get; init; }
    public required string ClientId { get; init; }

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
}
