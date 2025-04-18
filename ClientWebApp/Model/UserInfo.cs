using System.Text.Json.Serialization;

namespace ClientWebApp.Model;

public class UserInfo
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public bool IsParentTenant { get; set; }
    [JsonPropertyName("userName")]
    public string UserName { get; set; }
    public string LandingPage { get; set; }
    public IEnumerable<ClaimDto> Claims { get; set; }
}
