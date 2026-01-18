using IDP.Domain.AggregateRoots.Permissions;

namespace IDP.Domain.AggregateRoots.Roles;

public class RolePermission : BaseEntity
{
    public int RoleId { get; private set; }
    public int PermissionId { get; private set; }

    public string PermissionKey { get; private set; }
    public bool IsAllowed { get; private set; }

    public virtual Role Role { get; private set; }
    public virtual Permission Permission { get; private set; }

    private RolePermission() : base() { }

    internal RolePermission(
        int roleId,
        int permissionId,
        string permissionKey,
        bool isAllowed)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        PermissionKey = permissionKey;
        IsAllowed = isAllowed;
    }

    public void Set(bool isAllowed)
    {
        IsAllowed = isAllowed;
    }
}
