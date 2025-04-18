namespace Identity.Application.Roles.Queries;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string RoleDescription { get; set; }
    public bool? IsActive { get; set; }
    public bool ShowToTenant { get; set; }
    public bool? IsDeleted { get; set; }
}
