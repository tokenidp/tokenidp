using System.Text.Json.Serialization;

namespace ClientWebApp.Model;

public class TokenResponse
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }

    [JsonPropertyName("idToken")]
    public string IDToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("expiry")]
    public DateTime Expiry { get; set; }
}