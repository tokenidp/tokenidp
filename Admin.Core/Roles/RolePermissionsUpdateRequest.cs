using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Roles;

internal class RolePermissionsUpdateRequest
{
    [Required]
    public int[] PermissionIds { get; set; } = Array.Empty<int>();
}
