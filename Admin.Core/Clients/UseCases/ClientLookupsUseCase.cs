using Admin.Core.Common;

namespace Admin.Core.Clients.UseCases;

internal sealed class ClientLookupsUseCase
{
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ClientLookupsUseCase> _logger;

    public ClientLookupsUseCase(
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<ClientLookupsUseCase> logger)
    {
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<ClientLookups>> GetClientLookups(CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.LOOKUP}:client:{_currentUserService.TenantId}";

        _logger.LogDebug("Fetching client lookups for tenant {TenantId}", _currentUserService.TenantId);

        var lookups = await _cache.GetOrCreateAsync(
            cacheKey,
            () => Task.FromResult(new ClientLookups
            {
                AppTypes = ClientLookupMapper.MapAppTypes(),
                TokenTypes = ClientLookupMapper.MapTokenTypes(),
                ClientScopes = ClientLookupMapper.MapClientScopes(),
                GrantTypes = ClientLookupMapper.MapGrantTypes()
            }),
            TimeSpan.FromMinutes(10));

        _logger.LogDebug("Client lookups fetched for tenant {TenantId}", _currentUserService.TenantId);

        return ApiResult<ClientLookups>.Success(lookups);
    }
}