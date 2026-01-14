using IDP.Domain.AggregateRoots;
using System.Linq.Expressions;

namespace Admin.Core.Roles;

internal class PermissionParentDto
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;

    internal static Expression<Func<Permission, PermissionParentDto>> Projection =>
        permission => new PermissionParentDto
        {
            Id = permission.Id,
            Sequence = permission.Sequence,
            PermissionKey = permission.Permissionkey,
            PermissionName = permission.PermissionName
        };
}
