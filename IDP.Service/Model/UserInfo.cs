namespace IDP.Service.Model;

public class UserInfo
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; }
    public string LandingPage { get; private set; }
    public IEnumerable<ClaimDto> Claims { get; private set; }

    private UserInfo() { }

    public static UserInfo Create(
        int userId,
        int tenantId,
        string name,
        string page,
        IEnumerable<ClaimDto> claims)
    {
        return new UserInfo()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = name,
            LandingPage = page,
            Claims = claims,
        };
    }
}
