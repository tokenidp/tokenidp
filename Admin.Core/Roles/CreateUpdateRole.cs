using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Roles;

internal class CreateUpdateRole
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string RoleDescription { get; set; }
    public bool? IsActive { get; set; }
}
