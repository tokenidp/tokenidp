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
        DefaultTenant = "system"
    };

    [Theory]
    [InlineData("acme.tokenidp.com", "acme")]
    [InlineData("system.tokenidp.com", "system")]
    [InlineData("acme.localhost:5001", "acme")]
    [InlineData("system.localhost:5001", "system")]
    public void Resolve_ShouldParseTenantSubdomain(string host, string expectedTenantKey)
    {
        var result = TenantHostParser.Resolve(host, Options);

        result.Kind.Should().Be(TenantHostResolutionKind.Tenant);
        result.TenantKey.Should().Be(expectedTenantKey);
    }

    [Theory]
    [InlineData("tokenidp.com")]
    [InlineData("localhost:5001")]
    public void Resolve_ShouldRecognizeRootHost(string host)
    {
        var result = TenantHostParser.Resolve(host, Options);

        result.Kind.Should().Be(TenantHostResolutionKind.Root);
        result.TenantKey.Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldRejectNestedSubdomains()
    {
        var result = TenantHostParser.Resolve("a.b.tokenidp.com", Options);

        result.Kind.Should().Be(TenantHostResolutionKind.Invalid);
    }

    [Fact]
    public void Resolve_ShouldIgnoreUnknownHost()
    {
        var result = TenantHostParser.Resolve("evil.example.com", Options);

        result.Kind.Should().Be(TenantHostResolutionKind.None);
    }
}
