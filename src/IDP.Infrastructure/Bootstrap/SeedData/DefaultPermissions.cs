using Admin.Core.Permissions;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal class DefaultPermissions
{
    public static IReadOnlyCollection<CreateUpdatePermission> CreateDefaultPermissions(int tenantId)
    {
        List<CreateUpdatePermission> allPermissions = new();

        //NavLinks
        var usersView = CreatePermission(tenantId, "users.view", "Users", "NavGroup", 5, "/users", "fa-users me-2");
        var rolesView = CreatePermission(tenantId, "roles.view", "Roles", "NavGroup", 6, "/roles", "fa-shield-alt me-2");
        var permissionsView = CreatePermission(tenantId, "permissions.view", "Permissions", "NavGroup", 7, "/permissions", "fa-shield me-2");
        var tenantsView = CreatePermission(tenantId, "tenants.view", "Tenants", "NavGroup", 4, "/tenants", "fa-building me-2");

        //NavGroups
        //var userManagement = CreatePermission(tenantId, "user.management.view", "User Management", "NavGroup", 4, null, "fa-users-gear");
        //userManagement.ChildPermissions = new();
        //userManagement.ChildPermissions.AddRange(usersView, rolesView, permissionsView);

        allPermissions.Add(CreatePermission(tenantId, "dashboard.view", "Dashboard", "NavGroup", 1, "/dashboard", "fa-chart-line me-2"));
        allPermissions.Add(CreatePermission(tenantId, "applications.view", "Applications", "NavGroup", 2, "/applications", "fa-layer-group me-2"));
        allPermissions.Add(CreatePermission(tenantId, "apiresources.view", "API Resources", "NavGroup", 3, "/api-resources", "fa-network-wired me-2"));
        allPermissions.Add(tenantsView);
        allPermissions.Add(usersView);
        allPermissions.Add(rolesView);
        allPermissions.Add(permissionsView);
        //allPermissions.Add(userManagement);
        allPermissions.Add(CreatePermission(tenantId, "tokens.view", "Token Management", "NavGroup", 8, "/tokens", "fa-id-badge me-2"));
        allPermissions.Add(CreatePermission(tenantId, "activities.view", "Activities", "NavGroup", 9, "/activities", "fa-clipboard-list me-2"));
        allPermissions.Add(CreatePermission(tenantId, "settings.view", "Settings", "NavGroup", 10, "/settings", "fa-cog me-2"));

        //Actions
        int i = 11;
        foreach (var permission in allPermissions
            .Where(p => p.PermissionKey != "dashboard.view"
                && p.PermissionKey != "activities.view"
                && p.PermissionKey != "apiresources.view"))
        {
            if (permission.ChildPermissions == null || permission.ChildPermissions.Count == 0)
            {
                permission.ChildPermissions = new();

                string parent = permission.PermissionName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()!;
                string singular = parent.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? parent[..^1] : parent;

                if (!parent.EndsWith("s", StringComparison.OrdinalIgnoreCase))
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

                    if (permission.PermissionName.Contains("Users"))
                    {
                        ++i;
                        var resetPasswordPermission = CreateActionPermission(tenantId, i, "users.resetpassword", "Reset Users Password");

                        permission.ChildPermissions.Add(resetPasswordPermission);
                    }

                    if (permission.PermissionName.Contains("Roles"))
                    {
                        ++i;
                        var deletePermission = CreateActionPermission(tenantId, i, $"{parent.ToLower()}.delete", $"Delete {parent}");

                        permission.ChildPermissions.Add(deletePermission);
                    }
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
                }
            }
        }

        tenantsView.ChildPermissions ??= new List<CreateUpdatePermission>();

        tenantsView.ChildPermissions.Add(CreateActionPermission(
            tenantId,
            ++i,
            "tenant.secret.reveal",
            "Reveal Tenant Provider Secret"));

        var apiResourcesPermission = allPermissions.FirstOrDefault(x => x.PermissionKey == "apiresources.view");
        if (apiResourcesPermission is not null)
        {
            apiResourcesPermission.ChildPermissions ??= new List<CreateUpdatePermission>();
            apiResourcesPermission.ChildPermissions.Add(CreateActionPermission(
                tenantId,
                ++i,
                "apiresources.add",
                "Create API Resources",
                "/api-resources/new"));
            apiResourcesPermission.ChildPermissions.Add(CreateActionPermission(
                tenantId,
                ++i,
                "apiresources.edit",
                "Modify API Resources",
                "/api-resources/edit"));
            apiResourcesPermission.ChildPermissions.Add(CreateActionPermission(
                tenantId,
                ++i,
                "apiresources.delete",
                "Delete API Resources"));
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