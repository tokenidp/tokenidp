namespace Identity.Service.Security;

public class DynamicRolePolicyHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    private readonly IAuthorizationPolicyProvider _policyProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorization _authorization;

    public DynamicRolePolicyHandler(IAuthorizationPolicyProvider policyProvider,
        ICurrentUserService currentUserService,
        IAuthorization authorization)
    {
        _policyProvider = policyProvider;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return;

        if (httpContext.User?.Identity?.IsAuthenticated == false)
        {
            context.Fail();
            return;
        }

        // Get the Authorize attribute on the current action
        var endpoint = httpContext.GetEndpoint();
        var authorizeAttributes = endpoint.Metadata.GetOrderedMetadata<AuthorizeAttribute>();

        if (!authorizeAttributes.Any())
        {
            context.Succeed(requirement);
        }

        foreach (var authorizeAttribute in authorizeAttributes)
        {
            if (!await CheckPolicyRequirementsAsync(authorizeAttribute, context))
            {
                return;
            }

            //Check Role Requirements
            if (!string.IsNullOrEmpty(authorizeAttribute.Roles))
            {
                var roles = authorizeAttribute.Roles.Split(',').ToList();
                if (!roles.Exists(role => context.User.IsInRole(role.Trim())))
                {
                    context.Fail();
                    return;
                }
            }
        }

        context.Succeed(requirement);
    }

    private async Task<bool> CheckPolicyRequirementsAsync(
        AuthorizeAttribute authorizeAttribute,
        AuthorizationHandlerContext context)
    {
        if (string.IsNullOrEmpty(authorizeAttribute.Policy))
        {
            return true;
        }

        var policy = await _policyProvider.GetPolicyAsync(authorizeAttribute.Policy);
        if (policy == null)
        {
            return true;
        }

        foreach (var policyRequirement in policy.Requirements)
        {
            if (!await PolicyRequirementSatisfied(policyRequirement))
            {
                context.Fail();
                return false;
            }
        }

        return true;
    }

    private async Task<bool> PolicyRequirementSatisfied(IAuthorizationRequirement requirement)
    {
        if (requirement is not DynamicPermissionRequirement dynamicRequirement)
            return false;

        var isAuthorized = await _authorization.IsAuthorized(
            _currentUserService.UserId,
            dynamicRequirement.Permission);

        return isAuthorized;
    }
}
