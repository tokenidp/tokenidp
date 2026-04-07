using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Permissions.UseCases;

internal sealed class PermissionLookupsUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<PermissionLookupsUseCase> _logger;

    public PermissionLookupsUseCase(
        IPermissionRepository permissionRepository,
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<PermissionLookupsUseCase> logger)
    {
        _permissionRepository = permissionRepository;
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<PermissionLookups>> GetPermissionLookups(
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.LOOKUP}:permission:{_currentUserService.TenantId}";

        _logger.LogDebug("Fetching permission lookups for tenant {TenantId}", _currentUserService.TenantId);

        var lookups = await _cache.GetOrCreateAsync(
            cacheKey,
            () => _permissionRepository.GetPermissionLookupsAsync(_currentUserService.TenantId, cancellationToken),
            TimeSpan.FromMinutes(10));

        _logger.LogDebug("Permission lookups fetched for tenant {TenantId}", _currentUserService.TenantId);

        return ApiResult<PermissionLookups>.Success(lookups);
    }
}
