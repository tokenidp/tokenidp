using Admin.Core.Permissions;
using System.Linq;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal class DefaultPermissions
{
    public static IReadOnlyCollection<CreateUpdatePermission> CreateDefaultPermissions(int tenantId)
    {
        List<CreateUpdatePermission> allPermissions = new();

        //NavLinks
        var usersView = CreatePermission(tenantId, "users.view", "Users", "NavLink", 8, "/users", "fa-users me-2");
        var rolesView = CreatePermission(tenantId, "roles.view", "Roles", "NavLink", 9, "/roles", "fa-shield-alt me-2");
        var permissionsView = CreatePermission(tenantId, "permissions.view", "Permissions", "NavLink", 10, "/permissions", "fa-shield me-2");

        //NavGroups
        var userManagement = CreatePermission(tenantId, "user.management.view", "User Management", "NavGroup", 4, null, "fa-users-gear");
        userManagement.ChildPermissions = new();
        userManagement.ChildPermissions.AddRange(usersView, rolesView, permissionsView);

        allPermissions.Add(CreatePermission(tenantId, "dashboard.view", "Dashboard", "NavGroup", 1, "/dashboard", "fa-chart-line me-2"));
        allPermissions.Add(CreatePermission(tenantId, "applications.view", "Applications", "NavGroup", 2, "/applications", "fa-layer-group me-2"));
        allPermissions.Add(CreatePermission(tenantId, "tenants.view", "Tenant Management", "NavGroup", 3, "/tenants", "fa-building me-2"));
        allPermissions.Add(userManagement);
        allPermissions.Add(CreatePermission(tenantId, "tokens.view", "Token Management", "NavGroup", 4, "/tokens", "fa-id-badge me-2"));
        allPermissions.Add(CreatePermission(tenantId, "activities.view", "Activities", "NavGroup", 6, "/activities", "fa-clipboard-list me-2"));
        allPermissions.Add(CreatePermission(tenantId, "settings.view", "Settings", "NavGroup", 7, "/settings", "fa-cog me-2"));

        //Actions
        int i = 11;
        foreach (var permission in allPermissions
            .Where(p => p.PermissionName != "Dashboard" && p.PermissionName != "Activities"))
        {
            if (permission.ChildPermissions == null || permission.ChildPermissions.Count == 0)
            {
                permission.ChildPermissions = new();

                string parent = permission.PermissionName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()!;
                string singular = parent.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? parent[..^1] : parent;

                if(!parent.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    parent = parent.Insert(parent.Length, "s");
                }

                if (permission.PermissionName.Contains("Token"))
                {
                    ++i;
                    var revokeToken = CreateActionPermission(tenantId, i, $"token.revoke", $"Revoke {parent}", null);
                    ++i;
                    var expireToken = CreateActionPermission(tenantId, i, $"token.expire", $"Expire {parent}", null);

                    permission.ChildPermissions.AddRange(revokeToken, expireToken);
                }
                else
                {
                    ++i;
                    var addPermission = CreateActionPermission(tenantId, i, $"{parent.ToLower()}.add", $"Create {parent}", $"/{parent.ToLower()}/add{singular.ToLower()}");
                    ++i;
                    var editPermission = CreateActionPermission(tenantId, i, $"{parent.ToLower()}.edit", $"Modify {parent}", $"/{parent.ToLower()}/edit{singular.ToLower()}");

                    permission.ChildPermissions.AddRange(addPermission, editPermission);
                }
            }
            else
            {
                foreach (var childPermission in permission.ChildPermissions)
                {
                    childPermission.ChildPermissions = new();

                    string parent = childPermission.PermissionName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()!;
                    string singular = parent.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? parent[..^1] : parent;

                    ++i;
                    var addPermission = CreateActionPermission(tenantId, i, $"{parent.ToLower()}.add", $"Create {parent}", $"/{parent.ToLower()}/add{singular.ToLower()}");
                    ++i;
                    var editPermission = CreateActionPermission(tenantId, i, $"{parent.ToLower()}.edit", $"Modify {parent}", $"/{parent.ToLower()}/edit{singular.ToLower()}");

                    childPermission.ChildPermissions.AddRange(addPermission, editPermission);

                    if (childPermission.PermissionName.Contains("Roles"))
                    {
                        ++i;
                        var deletePermission = CreateActionPermission(tenantId, i, $"{parent.ToLower()}.delete", $"Delete {parent}");

                        childPermission.ChildPermissions.Add(deletePermission);
                    }
                }
            }
        }
        return allPermissions;
    }

    public static CreateUpdatePermission CreatePermission(int tenantId,
        string permissionKey,
        string permissionName,
        string controlType,
        int sequence,
        string? accessUrl = null,
        string? icon = null) => new
        (
           parentId: null,
           tenantId: tenantId,
           permissionKey: permissionKey,
           permissionName: permissionName,
           accessUrl: accessUrl,
           icon: icon,
           controlType: controlType,
           isActive: true,
           isSystem: true,
           sequence: sequence
        );

    public static CreateUpdatePermission CreateActionPermission(int tenantId,
        int sequence,
        string permissionKey,
        string permissionName,
        string? accessUrl = null) => new
        (
            parentId: null,
            tenantId: tenantId,
            permissionKey: permissionKey,
            permissionName: permissionName,
            accessUrl: accessUrl,
            icon: null,
            controlType: "Action",
            isActive: true,
            isSystem: true,
            sequence: sequence
        );
}
