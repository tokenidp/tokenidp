using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;

    public PermissionRepository(ApplicationDbContext dbContext, ICache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public Task<Permission?> GetByIdAsync(int permissionId, CancellationToken ct)
    {
        return _dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId, ct);
    }

    public async Task<IEnumerable<PermissionList>> GetActivePermissionsAsync(CancellationToken ct)
    {
        return await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive != false)
            .OrderBy(p => p.Sequence)
            .ThenBy(p => p.PermissionKey)
            .Select(PermissionList.Projection)
            .ToListAsync(ct);
    }

    public async Task<PaginatedList<PermissionList>> SearchPermissionsAsync(SearchData request, CancellationToken ct)
    {
        var query = _dbContext.Permissions.AsNoTracking();
        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();

        var controlTypeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "ControlType", StringComparison.OrdinalIgnoreCase));

        if (controlTypeCriteria != null &&
            Enum.TryParse<ControlTypes>(controlTypeCriteria.Value, true, out var controlType))
        {
            query = query.Where(p => p.ControlType == controlType);
        }

        var activeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Active", StringComparison.OrdinalIgnoreCase));

        if (activeCriteria != null)
        {
            var raw = activeCriteria.Value?.Trim();
            if (string.Equals(raw, "Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.IsActive);
            }
            else if (string.Equals(raw, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => !p.IsActive);
            }
        }

        criterias = criterias
            .Where(c =>
                !string.Equals(c.ColumnName, "ControlType", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(c.ColumnName, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Select(PermissionList.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }

    public Task<PermissionById?> GetPermissionDetailAsync(int permissionId, CancellationToken ct)
    {
        return _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.Id == permissionId)
            .Select(PermissionById.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PermissionLookups> GetPermissionLookupsAsync(int tenantId, CancellationToken ct)
    {
        var cacheKey = $"{CacheKeys.LOOKUP}:permission:{tenantId}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var parentMenus = await _dbContext.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive != false &&
                                (p.ControlType == ControlTypes.NavGroup || p.ControlType == ControlTypes.NavLink))
                    .OrderBy(p => p.Sequence)
                    .Select(p => new LookupItem
                    {
                        Key = p.Id.ToString(),
                        Value = p.PermissionName
                    })
                    .ToListAsync(ct);

                return new PermissionLookups
                {
                    ParentMenus = parentMenus,
                    ControlTypes = PermissionLookupMapper.MapControlTypes()
                };
            },
            TimeSpan.FromMinutes(10));
    }

    public Task<bool> PermissionKeyExistsAsync(string permissionKey, CancellationToken ct)
    {
        return _dbContext.Permissions.AnyAsync(
            p => p.PermissionKey.ToUpper() == permissionKey,
            ct);
    }

    public async Task<int> GetNextPermissionSequenceAsync(CancellationToken ct)
    {
        var currentSequence = await _dbContext.Permissions.MaxAsync(x => (int?)x.Sequence, ct) ?? 0;
        return currentSequence + 1;
    }

    public async Task<int> AddAsync(Permission permission, CancellationToken ct)
    {
        _dbContext.Permissions.Add(permission);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}
