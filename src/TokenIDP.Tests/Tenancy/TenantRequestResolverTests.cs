using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Server.Multitenancy;

namespace TokenIDP.Tests.Tenancy;

public sealed class TenantRequestResolverTests
{
    private static readonly TenantResolutionOptions TenantOptions = new()
    {
        AllowedRootDomains = new[] { "idp.domain.com" },
        AllowedDevelopmentHosts = new[] { "localhost" },
        DefaultTenant = "system",
        AllowHeaderInStaging = true,
        AllowQueryInStaging = true
    };

    [Fact]
    public void Production_ShouldResolveTenantFromHost()
    {
        var resolver = CreateResolver(Environments.Production);
        var httpContext = CreateHttpContext("acme.idp.domain.com");

        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.Resolved);
        result.TenantKey.Should().Be("acme");
        result.Source.Should().Be(TenantResolutionSource.Host);
    }

    [Fact]
    public void Production_ShouldUseDefaultTenantForRootHost()
    {
        var resolver = CreateResolver(Environments.Production);
        var httpContext = CreateHttpContext("idp.domain.com");

        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.Resolved);
        result.TenantKey.Should().Be("system");
        result.Source.Should().Be(TenantResolutionSource.Default);
    }

    [Fact]
    public void Staging_ShouldResolveTenantFromQuery()
    {
        var resolver = CreateResolver(Environments.Staging);
        var httpContext = CreateHttpContext(
            "tresorauth-fxh4budpa9hha4db.canadacentral-01.azurewebsites.net",
            "?tenant=acme");

        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.Resolved);
        result.TenantKey.Should().Be("acme");
        result.Source.Should().Be(TenantResolutionSource.Query);
    }

    [Fact]
    public void Staging_ShouldResolveTenantFromHeader()
    {
        var resolver = CreateResolver(Environments.Staging);
        var httpContext = CreateHttpContext("ashy-meadow-06d616803.7.azurestaticapps.net");
        httpContext.Request.Headers[TenantOptions.HeaderName] = "acme";

        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.Resolved);
        result.TenantKey.Should().Be("acme");
        result.Source.Should().Be(TenantResolutionSource.Header);
    }

    [Fact]
    public void Staging_ShouldRejectInvalidTenantValue()
    {
        var resolver = CreateResolver(Environments.Staging);
        var httpContext = CreateHttpContext(
            "tresorauth-fxh4budpa9hha4db.canadacentral-01.azurewebsites.net",
            "?tenant=Acme!");

        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.InvalidTenantKey);
    }

    [Fact]
    public void Staging_ShouldUseDefaultTenantWhenMissing()
    {
        var resolver = CreateResolver(Environments.Staging);
        var httpContext = CreateHttpContext("tresorauth-fxh4budpa9hha4db.canadacentral-01.azurewebsites.net");

        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.Resolved);
        result.TenantKey.Should().Be("system");
        result.Source.Should().Be(TenantResolutionSource.Default);
    }

    [Fact]
    public void ForwardedHost_ShouldResolveTenant_WhenRequestHostIsUpdatedByProxyMiddleware()
    {
        var resolver = CreateResolver(Environments.Staging);
        var httpContext = CreateHttpContext("acme.idp.domain.com");
        var result = resolver.Resolve(httpContext);

        result.Status.Should().Be(TenantRequestResolutionStatus.Resolved);
        result.TenantKey.Should().Be("acme");
        result.Source.Should().Be(TenantResolutionSource.Host);
        httpContext.Request.Host.Host.Should().Be("acme.idp.domain.com");
    }

    private static TenantRequestResolver CreateResolver(string environmentName)
    {
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

        return new TenantRequestResolver(
            Microsoft.Extensions.Options.Options.Create(TenantOptions),
            hostEnvironment.Object);
    }

    private static DefaultHttpContext CreateHttpContext(string host, string queryString = "")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.QueryString = new QueryString(queryString);
        return httpContext;
    }
}
