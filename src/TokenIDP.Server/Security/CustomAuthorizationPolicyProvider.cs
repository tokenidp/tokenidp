using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TokenIDP.Server.Security;

public class CustomAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public CustomAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        return GetOrCreatePolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    private async Task<AuthorizationPolicy> GetOrCreatePolicyAsync(string policyName)
    {
        var fallbackPolicy = await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        if (fallbackPolicy is not null)
        {
            return fallbackPolicy;
        }

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new DynamicPermissionRequirement(policyName))
            .Build();
    }
}


