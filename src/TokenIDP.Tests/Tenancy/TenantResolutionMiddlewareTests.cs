using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.OAuth;
using TokenIDP.Server.Middlewares;
using TokenIDP.Server.Multitenancy;

namespace TokenIDP.Tests.Tenancy;

public sealed class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldResolveTenantWithoutRequestAbortedCancellation()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new TenantResolutionOptions()),
            Mock.Of<ICache>(),
            Mock.Of<IAppLogger<TenantResolutionMiddleware>>(),
            Mock.Of<IHostEnvironment>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("system.example.com");

        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        httpContext.RequestAborted = aborted.Token;

        var requestResolver = new Mock<ITenantRequestResolver>();
        requestResolver
            .Setup(x => x.Resolve(httpContext))
            .Returns(new TenantRequestResolutionResult(
                TenantRequestResolutionStatus.Resolved,
                "system",
                TenantResolutionSource.Default));

        CancellationToken observedToken = default;
        var tenantResolver = new Mock<ITenantResolver>();
        tenantResolver
            .Setup(x => x.ResolveAsync("system", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => observedToken = token)
            .ReturnsAsync(new TenantContext(1, "system", true));

        var tenantContextAccessor = new TenantContextAccessor();

        await middleware.InvokeAsync(
            httpContext,
            requestResolver.Object,
            tenantResolver.Object,
            tenantContextAccessor);

        nextCalled.Should().BeTrue();
        observedToken.CanBeCanceled.Should().BeFalse();
    }
}
