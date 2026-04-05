namespace Admin.Core.Roles;

public class CreateUpdateRolePermission
{
    public required int PermissionId { get; set; }
    public required int RoleId { get; set; }
    public required string PermissionKey { get; set; }
    public required bool IsAllowed { get; set; }
}
