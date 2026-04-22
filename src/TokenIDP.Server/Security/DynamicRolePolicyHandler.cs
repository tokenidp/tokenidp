using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Server.Security;

public sealed class DynamicRolePolicyHandler : AuthorizationHandler<DynamicPermissionRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoleRepository _roleStore;
    private readonly IAppLogger<DynamicRolePolicyHandler> _logger;

    public DynamicRolePolicyHandler(ICurrentUserService currentUserService,
        IRoleRepository roleStore,
        IAppLogger<DynamicRolePolicyHandler> logger)
    {
        _currentUserService = currentUserService;
        _roleStore = roleStore;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DynamicPermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug(
                "Permission authorization skipped because the user is not authenticated. Permission={Permission}",
                requirement.Permission);
            return;
        }

        var result = await _roleStore.HasPermission(
            _currentUserService.UserId,
            requirement.Permission);

        if (result.Value)
        {
            _logger.LogDebug(
                "Permission authorization succeeded. UserId={UserId}, Permission={Permission}",
                _currentUserService.UserId,
                requirement.Permission);
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "Permission authorization failed. UserId={UserId}, Permission={Permission}, TenantId={TenantId}",
            _currentUserService.UserId,
            requirement.Permission,
            _currentUserService.TenantId);
    }
}


