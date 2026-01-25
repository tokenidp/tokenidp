using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Projections;

namespace IDP.Infrastructure.Persistence;

internal sealed class ClientStore : IClientStore
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ClientStore> _logger;

    public ClientStore(IApplicationDbContext dbContext,
        IAppLogger<ClientStore> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
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
}
