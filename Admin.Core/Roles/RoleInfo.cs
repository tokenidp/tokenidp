namespace Admin.Core.Roles;

internal class RoleInfo
{
    internal static Expression<Func<Role, RoleInfo>> Projection =>
        r => new RoleInfo
        {
            Id = r.Id,
            Name = r.Name,
            RoleDescription = r.RoleDescription,
            IsActive = r.IsActive,
            IsEditable = r.IsSystem,
            IsAssignableToExternalUsers = r.IsAssignableToExternalUsers,

            RolePermissions = r.RolePermissions
                .Select(p => new RolePermissionInfo
                {
                    TenantPermissionId = p.PermissionId,
                    IsAllowed = p.IsAllowed
                })
                .ToList()
        };

    public int Id { get; set; }
    public string? Name { get; set; }
    public string? RoleDescription { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEditable { get; set; }
    public bool IsAssignableToExternalUsers { get; set; }

    public IList<RolePermissionInfo> RolePermissions { get; set; }
        = new List<RolePermissionInfo>();
}