using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Core.Admin.Permissions.UseCases;

internal sealed class PermissionLookupsUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<PermissionLookupsUseCase> _logger;

    public PermissionLookupsUseCase(
        IApplicationDbContext dbContext,
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<PermissionLookupsUseCase> logger)
    {
        _dbContext = dbContext;
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
            async () =>
            {
                var parentMenus = await _dbContext.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive != false
                    && (p.ControlType == ControlTypes.NavGroup
                        || p.ControlType == ControlTypes.NavLink))
                    .OrderBy(p => p.Sequence)
                    .Select(p => new LookupItem
                    {
                        Key = p.Id.ToString(),
                        Value = p.PermissionName
                    })
                    .ToListAsync(cancellationToken);

                return new PermissionLookups
                {
                    ParentMenus = parentMenus,
                    ControlTypes = PermissionLookupMapper.MapControlTypes()
                };
            },
            TimeSpan.FromMinutes(10));

        _logger.LogDebug("Permission lookups fetched for tenant {TenantId}", _currentUserService.TenantId);

        return ApiResult<PermissionLookups>.Success(lookups);
    }
}
