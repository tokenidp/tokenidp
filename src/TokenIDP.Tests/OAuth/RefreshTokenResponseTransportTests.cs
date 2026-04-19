using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.OAuth;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Tests.OAuth;

public sealed class RefreshTokenResponseTransportTests
{
    private readonly RefreshTokenResponseTransport _sut;

    public RefreshTokenResponseTransportTests()
    {
        var cookieOptions = Options.Create(new RefreshTokenCookieOptions());
        var cookieService = new RefreshTokenCookieService(cookieOptions);
        var logger = new Mock<IAppLogger<RefreshTokenResponseTransport>>();

        _sut = new RefreshTokenResponseTransport(logger.Object, cookieService);
    }

    [Fact]
    public void Apply_ResponseMode_ShouldKeepRefreshTokenInJson_AndNotSetCookie()
    {
        var httpContext = new DefaultHttpContext();
        var response = CreateResponse(RefreshTokenDeliveryMode.Response);

        _sut.Apply(httpContext, response);

        response.RefreshToken.Should().Be("refresh-token");
        httpContext.Response.Headers["Set-Cookie"].Should().BeEmpty();
    }

    [Fact]
    public void Apply_CookieMode_ShouldSetCookie_AndRemoveRefreshTokenFromJson()
    {
        var httpContext = new DefaultHttpContext();
        var response = CreateResponse(RefreshTokenDeliveryMode.Cookie);

        _sut.Apply(httpContext, response);

        response.RefreshToken.Should().BeNull();

        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString()
            .ToLowerInvariant();
        setCookie.Should().Contain("tt_refresh=refresh-token");
        setCookie.Should().Contain("path=/token");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("samesite=strict");
    }

    [Fact]
    public void Apply_BothMode_ShouldSetCookie_AndKeepRefreshTokenInJson()
    {
        var httpContext = new DefaultHttpContext();
        var response = CreateResponse(RefreshTokenDeliveryMode.Both);

        _sut.Apply(httpContext, response);

        response.RefreshToken.Should().Be("refresh-token");
        httpContext.Response.Headers["Set-Cookie"].ToString()
            .Should().Contain("tt_refresh=refresh-token");
    }

    private static TokenResponse CreateResponse(RefreshTokenDeliveryMode mode)
    {
        var response = TokenResponse.Success(
            userId: 7,
            token: "access-token",
            expireIn: 3600,
            expiry: DateTime.UtcNow.AddHours(1),
            idToken: null);

        response.AddRefreshToken("refresh-token", mode);
        return response;
    }
}
