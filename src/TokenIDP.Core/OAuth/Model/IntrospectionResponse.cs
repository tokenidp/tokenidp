using System.Text.Json.Serialization;

namespace TokenIDP.Core.OAuth.Model;

public class IntrospectionResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("sub")]
    public string? Sub { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("exp")]
    public long? Exp { get; set; }

    [JsonPropertyName("iat")]
    public long? Iat { get; set; }

    [JsonPropertyName("iss")]
    public string? Iss { get; set; }

    [JsonPropertyName("uid")]
    public string? TenantId { get; set; }

    [JsonPropertyName("roles")]
    public string[]? Roles { get; set; }

    private IntrospectionResponse() { }

    public static IntrospectionResponse Inactive()
        => new() { Active = false };

    public static IntrospectionResponse ActiveResponse(
        string sub,
        string clientId,
        string tenantId,
        string? scope,
        string[] roles,
        DateTime expiresAtUtc,
        DateTime issuedAtUtc,
        string issuer)
    {
        return new IntrospectionResponse
        {
            Active = true,
            Sub = sub,
            ClientId = clientId,
            Scope = scope,
            TenantId = tenantId,
            Roles = roles,
            Exp = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds(),
            Iat = new DateTimeOffset(issuedAtUtc).ToUnixTimeSeconds(),
            Iss = issuer
        };
    }
}
