using TokenIDP.Core.Admin.Clients;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

public interface IClientProvisioningService
{
    Task<bool> ExistsAsync(ApplicationDbContext db, int tenantId, string clientId, CancellationToken ct);
    Task CreateAsync(ApplicationDbContext db, int tenantId, string clientId, CreateUpdateClient command, CancellationToken ct);
}

