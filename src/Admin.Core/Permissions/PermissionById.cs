using IDP.Domain.AggregateRoots.Permissions;

namespace Admin.Core.Permissions;

internal class PermissionById
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Sequence { get; set; }

    public string PermissionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;

    public string? AccessUrl { get; set; }
    public string? Icon { get; set; }

    public string ControlType { get; set; } = string.Empty;
    public string Active { get; set; } = string.Empty;

    internal static Expression<Func<Permission, PermissionById>> Projection =>
        p => new PermissionById
        {
            Id = p.Id,
            ParentId = p.ParentId,
            Sequence = p.Sequence,
            PermissionKey = p.PermissionKey,
            PermissionName = p.PermissionName,
            AccessUrl = p.AccessUrl,
            Icon = p.Icon,

            ControlType = p.ControlType.ToString(),

            Active = p.IsActive ? "Active" : "Inactive"
        };
}
