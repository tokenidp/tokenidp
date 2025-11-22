using System.Linq.Expressions;

namespace IDP.Core.Admin.Model.Roles;

internal class RoleDto
{
    internal static Expression<Func<Role, RoleDto>> Projection =>
         t => new RoleDto
         {
             Id = t.Id,
             Name = t.Name,
             RoleDescription = t.RoleDescription,
             IsActive = t.IsActive,
             IsEditable = t.IsEditable
         };

    public int Id { get; set; }
    public string? Name { get; set; }
    public string? RoleDescription { get; set; }
    public bool? IsActive { get; set; }
    public bool IsEditable { get; set; }
}
