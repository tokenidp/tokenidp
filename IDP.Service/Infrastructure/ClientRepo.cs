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
        var tokenType = _cache.GetValue<string>(cacheKey);

        if (!string.IsNullOrEmpty(tokenType))
        {
            _logger.LogDebug("Cache hit for Client Token Type {CacheKey}", cacheKey.Substring(0, 10));
            return (TokenType)Enum.Parse(typeof(TokenType), tokenType);
        }

        var storedTokenType = await _dbContext.Clients
            .Where(s => s.ClientId == clientId)
            .Select(s => s.AccessTokenType)
            .FirstOrDefaultAsync();

        if (tokenType == null)
        {
            _logger.LogWarning("Token type not found for client: {ClientId}", clientId.Substring(0, 5));
            return default;
        }

        _logger.LogDebug("Retrieved token type {TokenType} for client {ClientId}",
                        tokenType, clientId.Substring(0, 5));

        _cache.Add(cacheKey, storedTokenType.ToString());

        _logger.LogDebug("Cached token type for {CacheKey}", cacheKey);

        return storedTokenType;
    }

    public async Task<Client> GetClient(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId.Substring(0, 5));

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey(clientId);
        var client = _cache.GetValue<Client>(cacheKey);

        if (client != null)
        {
            _logger.LogDebug("Cache hit for Client {CacheKey}", cacheKey.Substring(0, 7));
            return client;
        }

        client = await _dbContext.Clients.Include(c => c.ClientScopes)
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IsActive);

        _logger.LogDebug("Retrieved client {ClientId}", clientId.Substring(0, 5));

        _cache.Add(cacheKey, client);

        _logger.LogDebug("Cached token type for {CacheKey}", cacheKey.Substring(0, 7));

        return client;
    }
}
