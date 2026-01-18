namespace Admin.Core.Roles;

internal class RoleList
{
    internal static Expression<Func<RoleSearch, RoleList>> Projection =>
         t => new RoleList
         {
             Id = t.Id,
             RoleName = t.RoleName,
             TenantName = t.TenantName,
             Active = t.Active,
             UpdateBy = t.UpdatedBy,
         };

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string RoleName { get; set; }
    public string Active { get; set; }
    public string UpdateBy { get; set; }
}
