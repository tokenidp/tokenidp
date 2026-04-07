using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.ApiResources;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class ApiResourceRepository : IApiResourceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ApiResourceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ApiResource?> GetAggregateAsync(Guid id, int tenantId, CancellationToken ct)
    {
        return _dbContext.ApiResources
            .Include(x => x.Scopes)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    }

    public Task<List<ApiResourceDetail>> GetApiResourcesAsync(int tenantId, CancellationToken ct)
    {
        return _dbContext.ApiResources
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.DisplayName)
            .Select(ApiResourceDetail.Projection)
            .ToListAsync(ct);
    }

    public Task<ApiResourceDetail?> GetApiResourceDetailAsync(int tenantId, Guid id, CancellationToken ct)
    {
        return _dbContext.ApiResources
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(ApiResourceDetail.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> ApiResourceNameExistsAsync(int tenantId, string name, Guid? excludeId, CancellationToken ct)
    {
        var normalized = name.Trim().ToLower();
        return _dbContext.ApiResources
            .AsNoTracking()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                x.Name.ToLower() == normalized,
                ct);
    }

    public async Task<IReadOnlyList<ApiResourceValidationItem>> GetEnabledApiResourcesAsync(
        int tenantId,
        IReadOnlyCollection<string> requestedApiResources,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken ct)
    {
        return await _dbContext.ApiResources
            .AsNoTracking()
            .Where(resource =>
                resource.TenantId == tenantId &&
                resource.Enabled &&
                (requestedApiResources.Contains(resource.Name) ||
                 resource.Scopes.Any(scope => scope.Enabled && requestedScopes.Contains(scope.Name))))
            .Select(resource => new ApiResourceValidationItem
            {
                Name = resource.Name,
                ScopeNames = resource.Scopes
                    .Where(scope => scope.Enabled)
                    .Select(scope => scope.Name)
                    .ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<bool> HasAssignedClientScopeAsync(int tenantId, IReadOnlyCollection<string> scopeNames, CancellationToken ct)
    {
        if (scopeNames.Count == 0)
        {
            return false;
        }

        return await (
            from clientScope in _dbContext.ClientScopes.AsNoTracking()
            join client in _dbContext.Clients.AsNoTracking() on clientScope.ClientId equals client.Id
            where client.TenantId == tenantId && scopeNames.Contains(clientScope.Scope)
            select clientScope.Id
        ).AnyAsync(ct);
    }

    public async Task<bool> HasAssignedClientsAsync(int tenantId, string apiResourceName, CancellationToken ct)
    {
        return await (
            from clientApiResource in _dbContext.ClientApiResources.AsNoTracking()
            join client in _dbContext.Clients.AsNoTracking() on clientApiResource.ClientId equals client.Id
            where client.TenantId == tenantId && clientApiResource.Name == apiResourceName
            select clientApiResource.Id
        ).AnyAsync(ct);
    }

    public async Task<int> AddAsync(ApiResource apiResource, CancellationToken ct)
    {
        _dbContext.ApiResources.Add(apiResource);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteAsync(ApiResource apiResource, CancellationToken ct)
    {
        _dbContext.ApiResources.Remove(apiResource);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    public async Task RenameClientApiResourceAssignmentsAsync(int tenantId, string oldName, string newName, CancellationToken ct)
    {
        var clientAssignments = await (
            from clientApiResource in _dbContext.ClientApiResources
            join client in _dbContext.Clients on clientApiResource.ClientId equals client.Id
            where client.TenantId == tenantId && clientApiResource.Name == oldName
            select clientApiResource
        ).ToListAsync(ct);

        foreach (var assignment in clientAssignments)
        {
            assignment.Rename(newName);
        }
    }

    public async Task RenameClientScopeAssignmentsAsync(int tenantId, string oldName, string newName, CancellationToken ct)
    {
        var assignedScopes = await (
            from clientScope in _dbContext.ClientScopes
            join client in _dbContext.Clients on clientScope.ClientId equals client.Id
            where client.TenantId == tenantId && clientScope.Scope == oldName
            select clientScope
        ).ToListAsync(ct);

        foreach (var clientScope in assignedScopes)
        {
            clientScope.Rename(newName);
        }
    }
}
