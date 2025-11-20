using System.Linq.Expressions;

namespace IDP.Core.Admin.Roles;

public class RoleSearchDto
{
    public static Expression<Func<RoleSearch, RoleSearchDto>> Projection =>
         t => new RoleSearchDto
         {
             Id = t.Id,
             RoleName = t.RoleName,
             TenantName = t.TenantName,
             Active = t.Active,
             UpdateBy = t.UpdateBy,
         };

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string RoleName { get; set; }
    public string Active { get; set; }
    public string UpdateBy { get; set; }
}
