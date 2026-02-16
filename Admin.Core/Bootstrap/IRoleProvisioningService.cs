using Admin.Core.Clients;
using Admin.Core.Roles;

namespace Admin.Core.Bootstrap;

public interface IRoleProvisioningService
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string roleName, CancellationToken ct);
    Task<Role> CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdateRole command, CancellationToken ct);
}
