using IDP.Core.Model;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Projections;

namespace IDP.Infrastructure.Persistence;

internal sealed class ClientStore : IClientStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ClientStore> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ClientStore(IApplicationDbContext dbContext,
        IAppLogger<ClientStore> logger,
        ICache cache,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<ClientValidationSnapshot> GetByClientId(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey(clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
            .Where(x => x.ClientId == clientId && x.IsActive)
            .Select(ClientProjection.ValidationSnapshot)
            .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return client;
        });

        _logger.LogDebug("Retrieved client {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientShortInfo> GetClientShortInfo(int clientId)
    {
        _logger.LogDebug("GetValidationClient: Checking is valid client for client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey(clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
            .Where(x => x.Id == clientId && x.IsActive)
            .Select(ClientShortInfoProjection.Projection)
            .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return client;
        });

        _logger.LogDebug("Retrieved client client validation result for {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientShortInfo> GetClientShortInfo(string clientId)
    {
        _logger.LogDebug("GetValidationClient: Checking is valid client for client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT_VALIDATION.FormatCacheKey(clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
            .Where(x => x.ClientId == clientId && x.IsActive)
            .Select(ClientShortInfoProjection.Projection)
            .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return client;
        });

        _logger.LogDebug("Retrieved client client validation result for {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientExpiringSecret> GetClientExpiringSecretsAsync(int daysAhead,
        CancellationToken ct)
    {
        var tenantId = _currentUserService.TenantId;
        var now = DateTime.UtcNow;
        var untilUtc = now.AddDays(daysAhead);

        var cacheKey = CacheKeys.DASHBOARD_EXPIRING_CLIENT_SECRETS
            .FormatCacheKey(tenantId, daysAhead);

        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var expiring = await (
                 from s in _dbContext.ClientSecrets
                 join c in _dbContext.Clients on s.ClientId equals c.Id
                 where c.TenantId == tenantId
                    && s.ExpiresAt <= untilUtc
                    && c.IsActive
                 group new { s, c } by new { s.ClientId, c.ClientName } into g
                 select new
                 {
                     g.Key.ClientId,
                     g.Key.ClientName,
                     ExpiresAtUtc = g.Min(x => x.s.ExpiresAt)
                 }
             ).ToListAsync(ct);

            var items = expiring.Select(x => new ClientExpiringSecretItem
            {
                ClientId = x.ClientId,
                ClientName = x.ClientName,
                ExpiresAtUtc = x.ExpiresAtUtc,
                DaysLeft = (int)Math.Ceiling((x.ExpiresAtUtc - now).TotalDays)
            }).ToList();

            return new ClientExpiringSecret
            {
                ExpiringClientCount = items.Count,
                Clients = items
                .OrderBy(x => x.DaysLeft)
                .ToList()
            };
        },
        expiration: TimeSpan.FromMinutes(15));
    }
}
