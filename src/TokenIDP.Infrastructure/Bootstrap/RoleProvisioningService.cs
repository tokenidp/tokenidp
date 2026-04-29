using TokenIDP.Core.Admin.Roles;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class RoleProvisioningService : IRoleProvisioningService
{
    public async Task<Role> CreateAsync(ApplicationDbContext db,
        int tenantId,
        CreateUpdateRole command,
        CancellationToken ct)
    {
        var role = new Role(
            tenantId: tenantId,
            name: command.RoleName,
            description: command.RoleDescription,
            isActive: command.IsActive ?? true,
            isEditable: false
        );

        var permissions = command.RolePermissions ?? new List<CreateUpdateRolePermission>();

        foreach (var permission in permissions)
        {
            var permissionResult = role.AddPermission(
                tenantPermissionId: permission.PermissionId,
                permissionKey: permission.PermissionKey,
                isAllowed: permission.IsAllowed,
                bypassEditableCheck: true
            );

            if (!permissionResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Failed to provision role permission '{permission.PermissionKey}': {FormatErrors(permissionResult)}");
            }
        }

        db.Roles.Add(role);

        await db.SaveChangesAsync(ct);

        return role;
    }

    public async Task<bool> ExistsAsync(ApplicationDbContext db,
        int tenantId,
        string roleName,
        CancellationToken ct)
    {
        var isExist = await db.Roles
                    .AsNoTracking()
                    .AnyAsync(t => t.TenantId == tenantId
                    && t.Name == roleName, ct);

        return isExist;
    }

    private static string FormatErrors(Result result)
    {
        return string.Join("; ", result.Errors.Select(x => x.Message));
    }
}

