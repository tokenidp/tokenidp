namespace IDP.Service.Infrastructure;

public class ClientRepo
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ClientRepo> _logger;

    public ClientRepo(ApplicationDbContext dbContext,
        IAppLogger<ClientRepo> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<TokenType> GetClientTokenType(string clientId)
    {
        _logger.LogDebug("Retrieving token type for client: {ClientId}", clientId.Substring(0, 5));

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("token", clientId);

        var tokenType = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            return await _dbContext.Clients
           .Where(s => s.ClientId == clientId)
           .Select(s => s.AccessTokenType)
           .FirstOrDefaultAsync();

        });

        _logger.LogDebug("Retrieved token type {TokenType} for client {ClientId}",
                        tokenType, clientId.Substring(0, 5));

        return tokenType;
    }

    public async Task<string> GetClientScopes(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId.Substring(0, 5));

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey(clientId);

        var scopes = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            var client = await _dbContext.Clients.Include(c => c.ClientScopes)
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IsActive);

            _logger.LogDebug("Cached token type for {CacheKey}", cacheKey.Substring(0, 7));

            return string.Join(" ", client.ClientScopes
           .Select(s => s.Scope).ToList());

        });

        _logger.LogDebug("Retrieved client {ClientId}", clientId.Substring(0, 5));

        return scopes;
    }
}
