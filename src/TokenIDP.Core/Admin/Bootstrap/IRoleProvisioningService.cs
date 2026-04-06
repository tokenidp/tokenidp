using TokenIDP.Core.Admin.Roles;

namespace TokenIDP.Core.Admin.Bootstrap;

public interface IRoleProvisioningService
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string roleName, CancellationToken ct);
    Task<Role> CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdateRole command, CancellationToken ct);
}

