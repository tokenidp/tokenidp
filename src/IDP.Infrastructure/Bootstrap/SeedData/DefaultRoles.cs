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

            if (permission.Children == null || permission.Children.Count == 0)
            {
                continue;
            }

            foreach (var child in permission.Children)
            {
                CreateUpdateRolePermission childPermission = new CreateUpdateRolePermission()
                {
                    PermissionId = child.Id,
                    RoleId = 0,
                    PermissionKey = child.PermissionKey,
                    IsAllowed = true
                };
                rolePermissions.Add(childPermission);

                foreach (var deeper in child.Children)
                {
                    CreateUpdateRolePermission deeperPermission = new CreateUpdateRolePermission()
                    {
                        PermissionId = deeper.Id,
                        RoleId = 0,
                        PermissionKey = deeper.PermissionKey,
                        IsAllowed = true
                    };
                    rolePermissions.Add(deeperPermission);
                }
            }
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
