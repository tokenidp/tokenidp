using FluentAssertions;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Infrastructure.Bootstrap.SeedData;

namespace TokenIDP.Tests.Tenancy;

public sealed class DefaultPermissionsTests
{
    [Fact]
    public void NonSystemTenantPermissions_ShouldExcludeTenantManagementAndSystemOnlyActions()
    {
        var permissions = DefaultPermissions.CreateDefaultPermissions(
            tenantId: 42,
            includeSystemTenantPermissions: false);

        var flattenedKeys = Flatten(permissions)
            .Select(permission => permission.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        flattenedKeys.Should().NotContain("tenants.view");
        flattenedKeys.Should().NotContain("tenants.edit");
        flattenedKeys.Should().NotContain("tenants.socialsignin.view");
        flattenedKeys.Should().NotContain("tenants.socialsignin.edit");
        flattenedKeys.Should().NotContain("tenant.secret.reveal");
        flattenedKeys.Should().NotContain("tenants.add");
        flattenedKeys.Should().NotContain("tenants.delete");
        flattenedKeys.Should().NotContain("applications.delete");
        flattenedKeys.Should().NotContain("users.delete");
    }

    private static IEnumerable<CreateUpdatePermission> Flatten(IEnumerable<CreateUpdatePermission> permissions)
    {
        foreach (var permission in permissions)
        {
            yield return permission;

            foreach (var child in Flatten(permission.ChildPermissions ?? Enumerable.Empty<CreateUpdatePermission>()))
            {
                yield return child;
            }
        }
    }
}
