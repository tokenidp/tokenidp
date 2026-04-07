using TokenIDP.Core.Admin.Users;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

public interface IUserProvisioningService
{
    Task<User> CreateAsync(ApplicationDbContext db, int tenantId, UserDetail command, CancellationToken ct);
    Task<User?> ExistsAsync(ApplicationDbContext db, int tenantId, string userName, CancellationToken ct);
}

