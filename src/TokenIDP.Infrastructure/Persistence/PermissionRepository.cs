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
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public PermissionRepository(
        ApplicationDbContext dbContext,
        ICache cache,
        ITenantContextAccessor tenantContextAccessor)
    {
        _dbContext = dbContext;
        _cache = cache;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public Task<Permission?> GetByIdAsync(int permissionId, CancellationToken ct)
    {
        return ApplyTenantPermissionVisibility(_dbContext.Permissions)
            .FirstOrDefaultAsync(p => p.Id == permissionId && !p.IsDeleted, ct);
    }

    public async Task<IEnumerable<PermissionList>> GetActivePermissionsAsync(CancellationToken ct)
    {
        return await ApplyTenantPermissionVisibility(_dbContext.Permissions)
            .AsNoTracking()
            .Where(p => p.IsActive != false && !p.IsDeleted)
            .OrderBy(p => p.Sequence)
            .ThenBy(p => p.PermissionKey)
            .Select(PermissionList.Projection)
            .ToListAsync(ct);
    }

    public async Task<PaginatedList<PermissionList>> SearchPermissionsAsync(SearchData request, CancellationToken ct)
    {
        var query = ApplyTenantPermissionVisibility(_dbContext.Permissions)
            .AsNoTracking()
            .Where(p => !p.IsDeleted);
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
        return ApplyTenantPermissionVisibility(_dbContext.Permissions)
            .AsNoTracking()
            .Where(p => p.Id == permissionId && !p.IsDeleted)
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
                var parentMenus = await ApplyTenantPermissionVisibility(_dbContext.Permissions)
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted &&
                                p.IsActive != false &&
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
            p => !p.IsDeleted && p.PermissionKey.ToUpper() == permissionKey,
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

    public async Task<int> DeleteAsync(Permission permission, CancellationToken ct)
    {
        var deleteResult = permission.SoftDelete();
        if (!deleteResult.IsSuccess)
        {
            throw new InvalidOperationException(
                string.Join("; ", deleteResult.Errors.Select(x => x.Message)));
        }

        _dbContext.Permissions.Update(permission);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    private IQueryable<Permission> ApplyTenantPermissionVisibility(IQueryable<Permission> query)
    {
        if (!_tenantContextAccessor.HasTenant || _tenantContextAccessor.IsSystemTenant)
        {
            return query;
        }

        return query.Where(p =>
            !p.PermissionKey.StartsWith("tenants.") &&
            p.PermissionKey != "tenant.secret.reveal");
    }
}
