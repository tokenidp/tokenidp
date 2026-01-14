namespace Admin.Core.Roles;

internal class PermissionDto
{
    public int Id { get; private set; }
    public int? ParentId { get; private set; }
    public int UserId { get; private set; }
    public int Sequence { get; private set; }
    public string PermissionType { get; private set; }
    public string PermissionName { get; private set; }
    public string PermissionValue { get; private set; }
    public string PermissionKey { get; private set; }
    public string? Icon { get; private set; }
    public string? Url { get; private set; }
    public string RoleName { get; private set; }
    public string ControlType { get; private set; }

    public PermissionDto(int permissionId,
        int? parentId,
        int userId,
        int sequence,
        string permissionType,
        string permissionName,
        string permissionValue,
        string permissionKey,
        string icon,
        string url,
        string roleName,
        string controlType)
    {
        Id = permissionId;
        ParentId = parentId;
        UserId = userId;
        Sequence = sequence;
        PermissionType = permissionType;
        PermissionName = permissionName;
        PermissionValue = permissionValue;
        PermissionKey = permissionKey;
        Icon = icon;
        Url = url;
        RoleName = roleName;
        ControlType = controlType;
    }
}
