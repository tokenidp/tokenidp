using FluentAssertions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Server.Multitenancy;

namespace TokenIDP.Tests.Tenancy;

public sealed class TenantHostParserTests
{
    private static readonly TenantResolutionOptions Options = new()
    {
        AllowedRootDomains = new[] { "tokenidp.com" },
        AllowedDevelopmentHosts = new[] { "localhost" },
        FallbackTenantKey = "system"
    };

    [Theory]
    [InlineData("acme.tokenidp.com", "acme")]
    [InlineData("system.tokenidp.com", "system")]
    [InlineData("acme.localhost:5001", "acme")]
    [InlineData("system.localhost:5001", "system")]
    [InlineData("tokenidp.com", "system")]
    public void TryResolveTenantKey_ShouldParseExpectedTenantKey(string host, string expectedTenantKey)
    {
        var resolved = TenantHostParser.TryResolveTenantKey(host, Options, out var tenantKey);

        resolved.Should().BeTrue();
        tenantKey.Should().Be(expectedTenantKey);
    }

    [Fact]
    public void TryResolveTenantKey_ShouldRejectUnknownHost()
    {
        var resolved = TenantHostParser.TryResolveTenantKey("evil.example.com", Options, out _);

        resolved.Should().BeFalse();
    }
}
