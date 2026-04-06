using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class DeviceAuthorizationRequest
{
    /// <summary>
    /// OAuth client identifier (public client allowed).
    /// </summary>
    [Required]
    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = default!;

    /// <summary>
    /// Requested OAuth scopes (space-delimited).
    /// </summary>
    [Required]
    [JsonPropertyName("scope")]
    public string Scope { get; init; } = default!;

    /// <summary>
    /// Optional PKCE code challenge (recommended for hardened device flows).
    /// </summary>
    [JsonPropertyName("code_challenge")]
    public string? CodeChallenge { get; init; }

    /// <summary>
    /// PKCE method (plain or S256).
    /// </summary>
    [JsonPropertyName("code_challenge_method")]
    public string? CodeChallengeMethod { get; init; }

    /// <summary>
    /// Optional device metadata (device name / model).
    /// </summary>
    [JsonPropertyName("device_metadata")]
    public string? DeviceMetadata { get; init; }
}