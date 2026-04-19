using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TokenIDP.Core.OAuth.RateLimiting;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.OAuth;

public sealed class OAuthClientRateLimitRequestResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldFallbackToIpPartition_WhenClientIdIsMissing()
    {
        var store = new Mock<IClientRateLimitPolicyStore>(MockBehavior.Strict);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/authorize";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.10.10.10");

        var sut = new OAuthClientRateLimitRequestResolver(store.Object);

        var result = await sut.ResolveAsync(httpContext, CancellationToken.None);

        result.ShouldRateLimit.Should().BeTrue();
        result.ClientId.Should().BeNull();
        result.PartitionKey.Should().Be("ip:10.10.10.10");
        result.PermitLimit.Should().Be(OAuthClientRateLimitRequestContext.DefaultPermitLimit);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUseClientPartition_WhenClientPolicyExists()
    {
        var store = new Mock<IClientRateLimitPolicyStore>();
        store
            .Setup(x => x.GetAsync("client-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientRateLimitProfile(
                "client-a",
                4,
                10,
                5,
                TimeSpan.FromMinutes(1)));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/authorize";
        httpContext.Request.QueryString = new QueryString("?client_id=client-a");
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.50");

        var sut = new OAuthClientRateLimitRequestResolver(store.Object);

        var result = await sut.ResolveAsync(httpContext, CancellationToken.None);

        result.PartitionKey.Should().Be("client:client-a");
        result.ClientId.Should().Be("client-a");
        result.TenantId.Should().Be(4);
        result.PermitLimit.Should().Be(10);
        result.QueueLimit.Should().Be(5);
        result.TimeWindow.Should().Be(TimeSpan.FromMinutes(1));
    }
}
