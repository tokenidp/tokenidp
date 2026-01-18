namespace Admin.Core.Permissions;

internal class CreateUpdatePermission
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? AccessUrl { get; set; }
    public string? Icon { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
