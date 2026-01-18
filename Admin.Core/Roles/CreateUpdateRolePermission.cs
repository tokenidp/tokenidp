namespace Admin.Core.Roles;

internal class CreateUpdateRolePermission
{
    public required int TenantPermissionId { get; set; }
    public required int RoleId { get; set; }
    public required string PermissionKey { get; set; }
    public required bool IsAllowed { get; set; }
}
