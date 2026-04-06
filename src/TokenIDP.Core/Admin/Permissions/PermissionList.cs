using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Core.Admin.Permissions;

internal class PermissionList
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Sequence { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? ControlType { get; set; }
    public string? Icon { get; set; }
    public string Active { get; set; } = string.Empty;

    internal static Expression<Func<Permission, PermissionList>> Projection =>
        permission => new PermissionList
        {
            Id = permission.Id,
            ParentId = permission.ParentId,
            Sequence = permission.Sequence,
            PermissionKey = permission.PermissionKey,
            PermissionName = permission.PermissionName,
            Url = permission.AccessUrl,
            ControlType = permission.ControlType.ToString(),
            Icon = permission.Icon,

            Active = permission.IsActive != false ? "Active" : "Inactive"
        };


}


