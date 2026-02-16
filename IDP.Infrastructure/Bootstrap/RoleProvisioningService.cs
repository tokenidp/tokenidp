using Admin.Core.Bootstrap;
using Admin.Core.Roles;

namespace IDP.Infrastructure.Bootstrap;

internal class RoleProvisioningService : IRoleProvisioningService
{
    public async Task<Role> CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdateRole command, CancellationToken ct)
    {
        var role = new Role(
            tenantId: tenantId,
            name: command.RoleName,
            description: command.RoleDescription,
            isActive: command.IsActive ?? true
        );

        var permissions = command.RolePermissions ?? new List<CreateUpdateRolePermission>();
        foreach (var permission in permissions)
        {
            var permissionResult = role.AddPermission(
                tenantPermissionId: permission.PermissionId,
                permissionKey: permission.PermissionKey,
                isAllowed: permission.IsAllowed
            );
        }

        db.Roles.Add(role);

        await db.SaveChangesAsync(ct);

        return role;
    }

    public async Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string roleName, CancellationToken ct)
    {
        var isExist = await db.Roles
                    .AsNoTracking()
                    .AnyAsync(t => t.TenantId == tenantId && t.Name == roleName, ct);

        return isExist;
    }
}
