namespace Admin.Core.Roles;

internal class PermissionCreateDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Sequence { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? AccessUrl { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
}
