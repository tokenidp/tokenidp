using Admin.Core.Roles;

namespace Admin.Core.Users;

internal class UserPermission
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; }
    public string LandingPage { get; private set; }
    public IEnumerable<PermissionDto> Permissions { get; private set; }

    private UserPermission() { }

    public static UserPermission Create(
        int userId,
        int tenantId,
        string name,
        string page,
        IEnumerable<PermissionDto> permissions)
    {
        return new UserPermission()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = name,
            LandingPage = page,
            Permissions = permissions
        };
    }
}
