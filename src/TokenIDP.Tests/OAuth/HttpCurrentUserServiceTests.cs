using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Server;

namespace TokenIDP.Tests.OAuth;

public sealed class HttpCurrentUserServiceTests
{
    [Fact]
    public void Claims_ShouldResolveOperationalAndAuthTenant_FromNewTokenShape()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("idp.example");
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "usr:42"),
                    new Claim("user_id", "42"),
                    new Claim("client_id", "idp-admin"),
                    new Claim("tenant_id", "22"),
                    new Claim("tenant_key", "smartdev"),
                    new Claim("auth_tenant_id", "1"),
                    new Claim("auth_tenant_key", "system")
                },
                "Bearer"));

        var sut = new HttpCurrentUserService(
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new TokenOptions { Issuer = "https://issuer.example" }));

        sut.UserId.Should().Be(42);
        sut.TenantId.Should().Be(22);
        sut.TenantKey.Should().Be("smartdev");
        sut.AuthTenantId.Should().Be(1);
        sut.AuthTenantKey.Should().Be("system");
        sut.ClientId.Should().Be("idp-admin");
    }
}
