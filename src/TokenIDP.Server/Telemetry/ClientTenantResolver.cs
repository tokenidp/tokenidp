using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Server.Telemetry;

internal sealed class ClientTenantResolver
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public ClientTenantResolver(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<int?> ResolveTenantIdAsync(string clientId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var cacheKey = $"telemetry:tenant:{clientId.Trim().ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out int? tenantId))
        {
            return tenantId;
        }

        tenantId = await _db.Clients
            .AsNoTracking()
            .Where(client => client.ClientId == clientId && !client.IsDeleted)
            .Select(client => (int?)client.TenantId)
            .FirstOrDefaultAsync(ct);

        _cache.Set(cacheKey, tenantId, TimeSpan.FromMinutes(10));
        return tenantId;
    }
}
