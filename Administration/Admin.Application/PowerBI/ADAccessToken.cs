using Newtonsoft.Json;

namespace Identity.Application.PowerBI;

public class ADAccessToken
{
    [JsonProperty("token_type")]
    public string TokenType { get; set; }
    [JsonProperty("expires_in")]
    public string ExpiresIn { get; set; }
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; }
    [JsonIgnore]
    public DateTime? ExpiryDate { get; private set; }

    public void SetExpire(string seconds)
    {
        ExpiryDate = DateTime.Now.AddSeconds(Double.Parse(seconds));
    }
}
