using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Server.Security;

public sealed class DynamicRolePolicyHandler : AuthorizationHandler<DynamicPermissionRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoleRepository _roleStore;

    public DynamicRolePolicyHandler(ICurrentUserService currentUserService,
        IRoleRepository roleStore)
    {
        _currentUserService = currentUserService;
        _roleStore = roleStore;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DynamicPermissionRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return;

        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var result = await _roleStore.HasPermission(
            _currentUserService.UserId,
            requirement.Permission);

        if (result.Value)
        {
            context.Succeed(requirement);
        }
    }
}


