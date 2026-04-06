using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.OAuth.Policies;

internal sealed class TenantUserMfaPolicy
{
    private readonly ITenantStore _tenantStore;

    public TenantUserMfaPolicy(ITenantStore tenantStore)
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


