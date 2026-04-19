using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.OAuth;

namespace TokenIDP.Tests.OAuth;

public sealed class RefreshTokenCookieServiceTests
{
    [Fact]
    public void Delete_ShouldWriteExpiredCookie_WithConfiguredNameAndPath()
    {
        var options = Options.Create(new RefreshTokenCookieOptions());
        var sut = new RefreshTokenCookieService(options);
        var httpContext = new DefaultHttpContext();

        sut.Delete(httpContext);

        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString()
            .ToLowerInvariant();
        setCookie.Should().Contain("tt_refresh=");
        setCookie.Should().Contain("path=/token");
        setCookie.Should().Contain("expires=");
    }
}
