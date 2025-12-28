namespace IDP.Core.OAuth.Model;

public class TokenRequest
{
    public required string GrantType { get; init; }
    public required string ClientId { get; init; }

    public string? Code { get; init; }
    public string? CodeVerifier { get; init; }
    public string? RedirectUri { get; init; }

    public string? RefreshToken { get; init; }
    public string? DeviceCode { get; init; }

    public string? Scope { get; init; }
    public string? IpAddress { get; set; }
}
