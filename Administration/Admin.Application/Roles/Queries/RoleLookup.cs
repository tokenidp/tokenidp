namespace Identity.Application.Roles.Queries;

public class RoleLookup : IMapFrom<AppRole>
{
    public int Id { get; set; }
    public string Name { get; set; }
}
