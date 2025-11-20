namespace IDP.Core.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class Permission : BaseEntity, IAggregateRoot
{
    public int? ParentId { get; private set; }
    public int Sequence { get; private set; }
    public string PermissionType { get; private set; }
    public string PermissionName { get; private set; }
    public string AccessUrl { get; private set; }
    public string Icon { get; private set; }
    public string ControlType { get; private set; }
    public bool IsEditable { get; private set; }
    public bool IsActive { get; private set; }

    public virtual ICollection<TenantPermission> TenantPermissions { get; private set; }

    private Permission() { }

    public Permission(int parentId,
        string claimType,
        string claimName,
        string accessUrl,
        string controlType,
        bool isEditable,
        bool isActive)
    {
        ParentId = parentId;
        PermissionType = claimType;
        PermissionName = claimName;
        AccessUrl = accessUrl;
        ControlType = controlType;
        IsEditable = isEditable;
        IsActive = isActive;
    }

    public void UpdateAppClaim(int parentId,
        string claimType,
        string claimName,
        bool isEditable,
        bool isActive)
    {
        ParentId = parentId;
        PermissionType = claimType;
        PermissionName = claimName;
        IsEditable = isEditable;
        IsActive = isActive;
    }
}
