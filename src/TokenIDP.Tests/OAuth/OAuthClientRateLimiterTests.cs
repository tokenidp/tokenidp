using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TokenIDP.Core.OAuth.RateLimiting;

namespace TokenIDP.Tests.OAuth;

public sealed class OAuthClientRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_ShouldQueueWithinConfiguredQueue_AndRejectBeyondQueueLimit()
    {
        using var limiter = new OAuthClientRateLimiter();
        var provider = BuildServices(new StaticResolver(
            permitLimit: 10,
            queueLimit: 5,
            timeWindow: TimeSpan.FromMinutes(1)));

        for (var index = 0; index < 10; index++)
        {
            var lease = await limiter.AcquireAsync(CreateContext(provider, "client-a"));
            lease.IsAcquired.Should().BeTrue();
        }

        var queuedTokens = Enumerable.Range(0, 5)
            .Select(_ => new CancellationTokenSource())
            .ToArray();
        var queuedTasks = queuedTokens
            .Select(token => limiter.AcquireAsync(
                CreateContext(provider, "client-a"),
                cancellationToken: token.Token).AsTask())
            .ToArray();

        await Task.Delay(50);
        queuedTasks.Should().OnlyContain(task => !task.IsCompleted);

        var rejectedLease = await limiter.AcquireAsync(CreateContext(provider, "client-a"));
        rejectedLease.IsAcquired.Should().BeFalse();

        foreach (var token in queuedTokens)
        {
            token.Cancel();
        }

        foreach (var queuedTask in queuedTasks)
        {
            try
            {
                await queuedTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    [Fact]
    public async Task AcquireAsync_ShouldIsolateDifferentClients()
    {
        using var limiter = new OAuthClientRateLimiter();
        var provider = BuildServices(new StaticResolver(
            permitLimit: 1,
            queueLimit: 0,
            timeWindow: TimeSpan.FromMinutes(1)));

        var firstClientLease = await limiter.AcquireAsync(CreateContext(provider, "client-a"));
        var secondClientLease = await limiter.AcquireAsync(CreateContext(provider, "client-b"));
        var rejectedLease = await limiter.AcquireAsync(CreateContext(provider, "client-a"));

        firstClientLease.IsAcquired.Should().BeTrue();
        secondClientLease.IsAcquired.Should().BeTrue();
        rejectedLease.IsAcquired.Should().BeFalse();
    }

    private static ServiceProvider BuildServices(IOAuthClientRateLimitRequestResolver resolver)
    {
        return new ServiceCollection()
            .AddSingleton(resolver)
            .BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider provider, string clientId)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        httpContext.Request.Path = "/authorize";
        httpContext.Request.QueryString = new QueryString($"?client_id={clientId}");
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        return httpContext;
    }

    private sealed class StaticResolver : IOAuthClientRateLimitRequestResolver
    {
        private readonly int _permitLimit;
        private readonly int _queueLimit;
        private readonly TimeSpan _timeWindow;

        public StaticResolver(int permitLimit, int queueLimit, TimeSpan timeWindow)
        {
            _permitLimit = permitLimit;
            _queueLimit = queueLimit;
            _timeWindow = timeWindow;
        }

        public ValueTask<OAuthClientRateLimitRequestContext> ResolveAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            var clientId = httpContext.Request.Query["client_id"].ToString();

            return ValueTask.FromResult(new OAuthClientRateLimitRequestContext(
                httpContext.Request.Path.Value ?? string.Empty,
                $"client:{clientId}",
                "127.0.0.1",
                clientId,
                1,
                _permitLimit,
                _queueLimit,
                _timeWindow,
                true));
        }
    }
}
