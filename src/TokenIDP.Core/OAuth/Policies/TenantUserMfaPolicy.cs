using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.Policies;

internal sealed class TenantUserMfaPolicy
{
    private readonly ITenantRepository _tenantStore;

    public TenantUserMfaPolicy(ITenantRepository tenantStore)
    {
        _tenantStore = tenantStore;
    }

    public async Task<bool> IsMfaRequiredAsync(AuthenticationContext context)
    {
        var tenantEnabled = await _tenantStore
            .CheckTwoFactorEnabled(context.User.TenantId);

        return tenantEnabled && context.User.TwoFactorEnabled;
    }
}



