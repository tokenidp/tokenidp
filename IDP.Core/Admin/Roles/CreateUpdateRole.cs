namespace IDP.Core.Admin.Roles;

public class CreateUpdateRole
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; }
    public string RoleDescription { get; set; }
    public bool? IsActive { get; set; }
}
