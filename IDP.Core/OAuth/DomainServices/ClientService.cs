using IDP.Core.Model;

namespace IDP.Core.OAuth.DomainServices;

internal sealed class ClientService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ClientService> _logger;

    public ClientService(ApplicationDbContext dbContext,
        IAppLogger<ClientService> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<ClientShortDto> GetClient(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey(clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
            .Where(x => x.ClientId == clientId && x.IsActive)
            .Select(ClientShortDto.Projection)
            .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return client;
        });

        _logger.LogDebug("Retrieved client {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientValidationResult> ValidateClient(string clientId)
    {
        _logger.LogDebug("IsValidClient: Checking is valid client for client: {ClientId}", clientId);

        var clientDto = await GetClient(clientId);

        return ClientValidationResult.Create(clientDto != null, clientDto?.Scopes ?? Array.Empty<string>());
    }
}