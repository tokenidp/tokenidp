using FluentAssertions;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Infrastructure.Bootstrap.SeedData;

namespace TokenIDP.Tests.Tenancy;

public sealed class DefaultPermissionsTests
{
    [Fact]
    public void NonSystemTenantPermissions_ShouldIncludeOwnTenantManagement_ButExcludeSystemOnlyActions()
    {
        var permissions = DefaultPermissions.CreateDefaultPermissions(
            tenantId: 42,
            includeSystemTenantPermissions: false);

        var flattenedKeys = Flatten(permissions)
            .Select(permission => permission.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        flattenedKeys.Should().Contain("tenants.view");
        flattenedKeys.Should().Contain("tenants.edit");
        flattenedKeys.Should().Contain("tenants.socialsignin.view");
        flattenedKeys.Should().Contain("tenants.socialsignin.edit");
        flattenedKeys.Should().Contain("tenant.secret.reveal");

        flattenedKeys.Should().NotContain("tenants.add");
        flattenedKeys.Should().NotContain("tenants.delete");
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
