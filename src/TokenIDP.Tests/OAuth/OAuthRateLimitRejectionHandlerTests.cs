using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using System.Text.Json;
using System.Threading.RateLimiting;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth.RateLimiting;

namespace TokenIDP.Tests.OAuth;

public sealed class OAuthRateLimitRejectionHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnOAuth429PayloadAndHeaders()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

        using var firstLease = await limiter.AcquireAsync();
        using var rejectedLease = await limiter.AcquireAsync();

        var httpContext = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        httpContext.Items[OAuthClientRateLimiter.ResponseMetadataKey] =
            new OAuthClientRateLimitResponseMetadata(
                "client-a",
                5,
                "127.0.0.1",
                "/token",
                10,
                5,
                TimeSpan.FromMinutes(1),
                0);

        var sut = new OAuthRateLimitRejectionHandler(
            Mock.Of<IAppLogger<OAuthRateLimitRejectionHandler>>());

        var context = new OnRejectedContext
        {
            HttpContext = httpContext,
            Lease = rejectedLease
        };

        await sut.HandleAsync(context, CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        httpContext.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("10");
        httpContext.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("0");
        httpContext.Response.Headers.RetryAfter.ToString().Should().NotBeNullOrWhiteSpace();

        responseBody.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<JsonElement>(responseBody);
        payload.GetProperty("error").GetString().Should().Be("rate_limit_exceeded");
    }
}
