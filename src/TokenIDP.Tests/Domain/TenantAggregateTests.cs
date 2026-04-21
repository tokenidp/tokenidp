using FluentAssertions;
using TokenIDP.Domain.AggregateRoots.Tenants;

namespace TokenIDP.Tests.DomainTests;

public sealed class TenantAggregateTests
{
    [Fact]
    public void Create_ShouldRejectReservedTenantKey_ForNonSystemTenant()
    {
        var result = Tenant.Create(
            "Acme",
            "system",
            "admin@acme.test",
            true,
            TenantAuthSetting.Create(0),
            TenantUISetting.Create("Light", null, "#000", "en", null),
            isSystemTenant: false,
            out var tenant);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == "tenant.key.reserved");
        tenant.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAllowSystemTenantKey_ForSystemTenant()
    {
        var result = Tenant.Create(
            "System",
            "system",
            "admin@system.test",
            true,
            TenantAuthSetting.Create(0),
            TenantUISetting.Create("Light", null, "#000", "en", null),
            isSystemTenant: true,
            out var tenant);

        result.IsSuccess.Should().BeTrue();
        tenant.Should().NotBeNull();
        tenant!.IsSystemTenant.Should().BeTrue();
        tenant.TenantKey.Should().Be("system");
    }
}
