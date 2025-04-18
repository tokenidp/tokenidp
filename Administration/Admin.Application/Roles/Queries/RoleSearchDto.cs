namespace Identity.Application.Roles.Queries;

public class RoleSearchDto : IMapFrom<RoleSearch>
{
    public int Id { get; set; }
    public string TenantName { get; set; }
    public string RoleName { get; set; }
    public string Active { get; set; }
    public string UpdateBy { get; set; }
}
