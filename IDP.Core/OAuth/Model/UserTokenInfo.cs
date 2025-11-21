namespace IDP.Core.OAuth.Model;

public class UserTokenInfo
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; }
    public string ClientId { get; private set; }
    public string[] Roles { get; private set; }

    private UserTokenInfo() { }

    public static UserTokenInfo Create(int userId,
        int tenantId,
        string userName,
        string clientId,
        string[] roles)
    {
        return new UserTokenInfo()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = userName,
            ClientId = clientId,
            Roles = roles
        };
    }
}
