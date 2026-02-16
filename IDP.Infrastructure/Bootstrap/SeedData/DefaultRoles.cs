using Admin.Core.Roles;
using IDP.Domain.AggregateRoots.Permissions;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal class DefaultRoles
{
    public static CreateUpdateRole CreateRole(List<Permission> permissions)
    {
        List<CreateUpdateRolePermission> rolePermissions = new();
        foreach (var permission in permissions)
        {
            CreateUpdateRolePermission rolePermission = new CreateUpdateRolePermission()
            {
                PermissionId = permission.Id,
                RoleId = 0,
                PermissionKey = permission.PermissionKey,
                IsAllowed = true
            };

            rolePermissions.Add(rolePermission);
        }

        CreateUpdateRole adminRole = new()
        {
            RoleName = "Administrator",
            RoleDescription = "Full administrative access to all IDP features",
            IsActive = true,
            RolePermissions = rolePermissions
        };

        return adminRole;
    }
}
