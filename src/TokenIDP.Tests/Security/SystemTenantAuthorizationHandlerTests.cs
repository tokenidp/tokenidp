using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.OAuth;
using TokenIDP.Server.Security;
using TokenIDP.Tests.OAuth;

namespace TokenIDP.Tests.Security;

public sealed class SystemTenantAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_ShouldSucceed_ForSystemTenantRequest()
    {
        var tenantContextAccessor = new TenantContextAccessor();
        tenantContextAccessor.SetTenant(new TenantContext(1, "system", true));

        var currentUserService = new TestCurrentUserService
        {
            UserId = 7,
            TenantId = 1
        };

        var handler = new SystemTenantAuthorizationHandler(currentUserService, tenantContextAccessor);
        var requirement = new SystemTenantRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
