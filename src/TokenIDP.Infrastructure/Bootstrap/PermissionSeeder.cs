using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class PermissionSeeder : IPermissionSeeder
{
    public async Task<Permission> CreateAsync(ApplicationDbContext db,
        int tenantId,
        CreateUpdatePermission command,
        CancellationToken ct)
    {
        var permission = new Permission(
            tenantId: tenantId,
            parentId: command.ParentId,
            sequence: command.Sequence,
            permissionKey: command.PermissionKey,
            permissionName: command.PermissionName,
            accessUrl: command.AccessUrl,
            icon: command.Icon,
            controlType: command.ControlType,
            isActive: true
        );

        if (command.ChildPermissions == null || command.ChildPermissions.Count == 0)
        {
            db.Permissions.Add(permission);
            await db.SaveChangesAsync(ct);

            return permission;
        }

        foreach (var child in command.ChildPermissions)
        {
            var childPermission = new Permission(
                        tenantId: tenantId,
                        parentId: child.ParentId,
                        sequence: child.Sequence,
                        permissionKey: child.PermissionKey,
                        permissionName: child.PermissionName,
                        accessUrl: child.AccessUrl,
                        icon: child.Icon,
                        controlType: child.ControlType,
                        isActive: true
            );
            permission.AddChild(childPermission);

            if (child.ChildPermissions == null || child.ChildPermissions.Count == 0)
                continue;

            foreach (var deeper in child.ChildPermissions)
            {
                childPermission.AddChild(
                    new Permission(
                            tenantId: tenantId,
                            parentId: deeper.ParentId,
                            sequence: deeper.Sequence,
                            permissionKey: deeper.PermissionKey,
                            permissionName: deeper.PermissionName,
                            accessUrl: deeper.AccessUrl,
                            icon: deeper.Icon,
                            controlType: deeper.ControlType,
                            isActive: true
                ));
            }
        }

        db.Permissions.Add(permission);
        await db.SaveChangesAsync(ct);

        return permission;
    }

    public async Task<bool> ExistsAsync(ApplicationDbContext db,
        int tenantId,
        string permissionKey,
        CancellationToken ct)
    {
        var isExist = await db.Permissions
                    .AsNoTracking()
                    .AnyAsync(t => t.TenantId == tenantId && t.PermissionKey == permissionKey, ct);

        return isExist;
    }
}


