using IDP.Domain.AggregateRoots;
using System.Linq.Expressions;

namespace Admin.Core.Roles;

internal class PermissionListDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Sequence { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? AccessUrl { get; set; }
    public string? ControlType { get; set; }
    public string? Icon { get; set; }

    internal static Expression<Func<Permission, PermissionListDto>> Projection =>
        permission => new PermissionListDto
        {
            Id = permission.Id,
            ParentId = permission.ParentId,
            Sequence = permission.Sequence,
            PermissionKey = permission.Permissionkey,
            PermissionName = permission.PermissionName,
            AccessUrl = permission.AccessUrl,
            ControlType = permission.ControlType,
            Icon = permission.Icon
        };
}
