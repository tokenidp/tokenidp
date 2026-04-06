using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Domain.AggregateRoots.Roles;

public class RolePermission : Entity<int>
{
    public int RoleId { get; private set; }
    public int PermissionId { get; private set; }

    public string PermissionKey { get; private set; } = default!;
    public bool IsAllowed { get; private set; }

    public virtual Role Role { get; private set; } = default!;
    public virtual Permission Permission { get; private set; } = default!;

    private RolePermission() : base() { }

    internal RolePermission(
        int permissionId,
        string permissionKey,
        bool isAllowed)
    {
        PermissionId = permissionId;
        PermissionKey = permissionKey;
        IsAllowed = isAllowed;
    }

    public void Set(bool isAllowed)
    {
        IsAllowed = isAllowed;
    }
}

