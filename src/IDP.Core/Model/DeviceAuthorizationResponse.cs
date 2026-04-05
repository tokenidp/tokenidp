namespace IDP.Core.Model;

using System.Text.Json.Serialization;

public sealed class DeviceAuthorizationResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; init; } = default!;

    [JsonPropertyName("user_code")]
    public string UserCode { get; init; } = default!;

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; init; } = default!;

    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("interval")]
    public int Interval { get; init; }
}
