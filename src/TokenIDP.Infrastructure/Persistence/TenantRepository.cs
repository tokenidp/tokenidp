using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Tenants;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<TenantRepository> _logger;

    public TenantRepository(ApplicationDbContext dbContext,
        IAppLogger<TenantRepository> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> CheckTwoFactorEnabled(int tenantId)
    {
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("TwoFactor", tenantId);

        var hasTwoFactorEnabled = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _dbContext.Tenants.Where(t => t.Id == tenantId)
            .Select(s => s.TenantAuthSetting.TwoFactor.IsEnabled)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        return hasTwoFactorEnabled;
    }

    public async Task<TenantUISetting?> GetTenantUISettings(int tenantId)
    {
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("UI", tenantId);

        var uiSetting = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _dbContext.TenantUISettings
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        return uiSetting;
    }

    public async Task<TenantExternalProvider?> ResolveExternalProvider(
       int tenantId,
       ExternalProviderTypes providerType,
       CancellationToken ct = default)
    {
        var provider = await _dbContext.TenantExternalProviders
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId
                  && x.ProviderType == providerType
                  && x.Enabled,
                ct);

        if (provider == null)
            return null;

        return provider;
    }

    public async Task<Tenant?> GetTenantAggregateAsync(int tenantId, CancellationToken ct)
    {
        return await _dbContext.Tenants
            .Include(t => t.TenantUISetting)
            .Include(t => t.TenantAuthSetting)
            .Include(t => t.TenantExternalProviders)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
    }

    public Task<TenantDetail?> GetTenantDetailAsync(int tenantId, CancellationToken ct)
    {
        return _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(TenantDetail.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public Task<Tenant?> GetTenantWithProvidersAsync(int tenantId, CancellationToken ct)
    {
        return _dbContext.Tenants
            .AsNoTracking()
            .Include(t => t.TenantExternalProviders)
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
    }

    public async Task<PaginatedList<TenantSearchResult>> SearchTenantsAsync(int? scopedTenantId, SearchData request, CancellationToken ct)
    {
        var query = _dbContext.Tenants.AsNoTracking();
        if (scopedTenantId.HasValue && scopedTenantId.Value > 0)
        {
            query = query.Where(t => t.Id == scopedTenantId.Value);
        }

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(tenant =>
                (tenant.TenantName ?? string.Empty).ToLower().Contains(term) ||
                (tenant.TenantCode ?? string.Empty).ToLower().Contains(term) ||
                (tenant.Email ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase));
        if (statusCriteria != null)
        {
            criterias = criterias
                .Where(c =>
                    !string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var raw = statusCriteria.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (bool.TryParse(raw, out var isActive))
                {
                    query = query.Where(tenant => tenant.IsActive == isActive);
                }
                else if (string.Equals(raw, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(tenant => tenant.IsActive == true);
                }
                else if (string.Equals(raw, "Inactive", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(raw, "Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(tenant => tenant.IsActive == false);
                }
            }
        }

        return await query
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .Select(TenantSearchResult.Projection)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }

    public Task<bool> TenantNameExistsAsync(string tenantName, int? excludeTenantId, CancellationToken ct)
    {
        var normalized = tenantName.ToLower();
        return _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t =>
                (!excludeTenantId.HasValue || t.Id != excludeTenantId.Value) &&
                t.TenantName.ToLower() == normalized,
                ct);
    }

    public Task<bool> TenantKeyExistsAsync(string tenantKey, CancellationToken ct)
    {
        var normalized = tenantKey.ToLower();
        return _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.TenantKey.ToLower() == normalized, ct);
    }

    public async Task<IReadOnlySet<int>> GetTenantExternalProviderIdsAsync(int tenantId, CancellationToken ct)
    {
        var providerIds = await _dbContext.TenantExternalProviders
            .AsNoTracking()
            .Where(provider => provider.TenantId == tenantId)
            .Select(provider => provider.Id)
            .ToListAsync(ct);

        return providerIds.ToHashSet();
    }

    public async Task<int> AddAsync(Tenant tenant, CancellationToken ct)
    {
        _dbContext.Tenants.Add(tenant);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    public Task<TenantAuthSetting?> GetTenantAuthSettingAsync(int tenantId, CancellationToken ct)
    {
        return _dbContext.TenantAuthSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
    }

    public async Task<TenantSummary?> GetSummaryAsync(int tenantId, CancellationToken ct)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantSummary
            {
                Id = t.Id,
                TenantName = t.TenantName ?? string.Empty,
                TenantDisplayName = t.TenantDisplayName ?? string.Empty,
                TenantKey = t.TenantKey ?? string.Empty
            })
            .FirstOrDefaultAsync(ct);
    }
}


