using Admin.Core.Common;

namespace Admin.Core.Clients.UseCases;

internal sealed class ClientLookupsUseCase
{
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ClientLookupsUseCase> _logger;
    private readonly IApplicationDbContext _appDbContext;

    public ClientLookupsUseCase(
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<ClientLookupsUseCase> logger,
        IApplicationDbContext appDbContext)
    {
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
        _appDbContext = appDbContext;
    }

    public async Task<ApiResult<ClientLookups>> GetClientLookups(CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.LOOKUP}:client:{_currentUserService.TenantId}";

        _logger.LogDebug("Fetching client lookups for tenant {TenantId}", _currentUserService.TenantId);

        var lookups = await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                return new ClientLookups
                {
                    AppTypes = ClientLookupMapper.MapAppTypes(),
                    TokenTypes = ClientLookupMapper.MapTokenTypes(),
                    ClientScopes = ClientLookupMapper.MapClientScopes(),
                    GrantTypes = ClientLookupMapper.MapGrantTypes(),
                    ExternalProviders = await ClientLookupMapper
                        .MapExternalProviders(_currentUserService.TenantId, _appDbContext),
                    Roles = await ClientLookupMapper.MapRoles(_currentUserService.TenantId, _appDbContext)
                };
            }, TimeSpan.FromMinutes(10));



        _logger.LogDebug("Client lookups fetched for tenant {TenantId}", _currentUserService.TenantId);

        return ApiResult<ClientLookups>.Success(lookups);
    }
}