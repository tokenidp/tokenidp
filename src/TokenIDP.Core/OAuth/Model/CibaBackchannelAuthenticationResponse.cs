using System.Text.Json.Serialization;

namespace TokenIDP.Core.OAuth.Model;

public sealed class CibaBackchannelAuthenticationResponse
{
    [JsonPropertyName("auth_req_id")]
    public string AuthReqId { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("interval")]
    public int? Interval { get; init; }
}
