using IDP.Core.Admin.Model.Roles;

namespace IDP.Core.Admin.Model.Users;

public class UserClaim
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; }
    public string LandingPage { get; private set; }
    public IEnumerable<ClaimDto> Claims { get; private set; }

    private UserClaim() { }

    public static UserClaim Create(
        int userId,
        int tenantId,
        string name,
        string page,
        IEnumerable<ClaimDto> claims)
    {
        return new UserClaim()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = name,
            LandingPage = page,
            Claims = claims,
        };
    }
}
