namespace TokenIDP.Core.Admin.Roles;

public class RoleList
{
    public static Expression<Func<RoleSearch, RoleList>> Projection =>
         t => new RoleList
         {
             Id = t.Id,
             RoleName = t.RoleName,
             Active = t.Active,
             UpdateBy = (t.FirstName ?? string.Empty) + " " + (t.LastName ?? string.Empty),
         };

    public int Id { get; set; }
    public string RoleName { get; set; } = default!;
    public string Active { get; set; } = default!;
    public string UpdateBy { get; set; } = default!;
}

