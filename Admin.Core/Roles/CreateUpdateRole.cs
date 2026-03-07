namespace Admin.Core.Roles;

public class CreateUpdateRole
{
    public int Id { get; set; }
    public required string RoleName { get; set; }
    public required string RoleDescription { get; set; }
    public bool? IsActive { get; set; }
    public bool IsAssignableToExternalUsers { get; set; } = false;

    public required IList<CreateUpdateRolePermission> RolePermissions { get; set; }
}