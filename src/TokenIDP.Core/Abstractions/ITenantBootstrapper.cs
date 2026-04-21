using TokenIDP.Core.Admin.Tenants;

namespace TokenIDP.Core.Abstractions;

public interface ITenantBootstrapper
{
    Task<TenantBootstrapResult> BootstrapAsync(CreateUpdateTenant command, CancellationToken cancellationToken);
}
