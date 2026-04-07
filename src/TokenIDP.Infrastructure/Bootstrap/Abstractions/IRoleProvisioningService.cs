using TokenIDP.Core.Admin.Roles;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

public interface IRoleProvisioningService
{
    Task<bool> ExistsAsync(ApplicationDbContext db, int tenantId, string roleName, CancellationToken ct);
    Task<Role> CreateAsync(ApplicationDbContext db, int tenantId, CreateUpdateRole command, CancellationToken ct);
}

