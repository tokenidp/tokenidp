using Microsoft.AspNetCore.Authorization;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Server.Security;

public sealed class SystemTenantAuthorizationHandler
    : AuthorizationHandler<SystemTenantRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public SystemTenantAuthorizationHandler(
        ICurrentUserService currentUserService,
        ITenantContextAccessor tenantContextAccessor)
    {
        _currentUserService = currentUserService;
        _tenantContextAccessor = tenantContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SystemTenantRequirement requirement)
    {
        if (_tenantContextAccessor.HasTenant &&
            _tenantContextAccessor.IsSystemTenant &&
            _currentUserService.TenantId > 0 &&
            _currentUserService.TenantId == _tenantContextAccessor.TenantId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
