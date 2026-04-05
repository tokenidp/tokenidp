using Admin.Core.Permissions;

namespace Admin.Core.Users;

internal class UserPermission
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; } = default!;
    public IEnumerable<PermissionInfo> Permissions { get; private set; } = default!;

    private UserPermission() { }

    public static UserPermission Create(
        int userId,
        int tenantId,
        string name,
        IEnumerable<PermissionInfo> permissions)
    {
        return new UserPermission()
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = name,
            Permissions = permissions
        };
    }
}