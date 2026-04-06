namespace TokenIDP.Core.Admin.Permissions;

public class CreateUpdatePermission
{
    public CreateUpdatePermission(int? parentId,
        int tenantId,
        string permissionKey,
        string permissionName,
        string? accessUrl,
        string? icon,
        string controlType,
        bool isActive,
        bool isSystem,
        int sequence)
    {
        ParentId = parentId;
        PermissionKey = permissionKey;
        PermissionName = permissionName;
        AccessUrl = accessUrl;
        Icon = icon;
        ControlType = controlType;
        IsActive = isActive;
        IsSystem = isSystem;
        TenantId = tenantId;
        Sequence = sequence;
    }

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? ParentId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? AccessUrl { get; set; }
    public string? Icon { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public int Sequence { get; set; }

    public List<CreateUpdatePermission> ChildPermissions { get; set; } = default!;
}

