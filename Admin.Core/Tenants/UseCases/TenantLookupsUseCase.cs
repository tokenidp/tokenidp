using Admin.Core.Common;

namespace Admin.Core.Tenants.UseCases;

internal sealed class TenantLookupsUseCase
{
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TenantLookupsUseCase> _logger;

    public TenantLookupsUseCase(
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<TenantLookupsUseCase> logger)
    {
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<TenantLookups>> GetTenantLookups(CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.LOOKUP}:tenant:{_currentUserService.TenantId}";

        _logger.LogDebug("Fetching tenant lookups for tenant {TenantId}", _currentUserService.TenantId);

        var lookups = await _cache.GetOrCreateAsync(
            cacheKey,
            () => Task.FromResult(new TenantLookups
            {
                Statuses = TenantLookupMapper.MapTenantStatuses(),
                ExternalProviders = TenantLookupMapper.MapExternalProviders(),
                AuthenticationModes = TenantLookupMapper.MapAuthenticationModes(),
                Themes = TenantLookupMapper.MapTenantThemes()
            }),
            TimeSpan.FromMinutes(10));

        _logger.LogDebug("Tenant lookups fetched for tenant {TenantId}", _currentUserService.TenantId);

        return ApiResult<TenantLookups>.Success(lookups);
    }
}