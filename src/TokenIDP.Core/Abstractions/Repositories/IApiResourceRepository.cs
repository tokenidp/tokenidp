using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.ApiResources;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IApiResourceRepository
{
    Task<ApiResource?> GetAggregateAsync(Guid id, int tenantId, CancellationToken ct);
    Task<List<ApiResourceDetail>> GetApiResourcesAsync(int tenantId, CancellationToken ct);
    Task<ApiResourceDetail?> GetApiResourceDetailAsync(int tenantId, Guid id, CancellationToken ct);
    Task<bool> ApiResourceNameExistsAsync(int tenantId, string name, Guid? excludeId, CancellationToken ct);
    Task<IReadOnlyList<ApiResourceValidationItem>> GetEnabledApiResourcesAsync(
        int tenantId,
        IReadOnlyCollection<string> requestedApiResources,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken ct);
    Task<bool> HasAssignedClientScopeAsync(int tenantId, IReadOnlyCollection<string> scopeNames, CancellationToken ct);
    Task<bool> HasAssignedClientsAsync(int tenantId, string apiResourceName, CancellationToken ct);
    Task<int> AddAsync(ApiResource apiResource, CancellationToken ct);
    Task<int> DeleteAsync(ApiResource apiResource, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task RenameClientApiResourceAssignmentsAsync(int tenantId, string oldName, string newName, CancellationToken ct);
    Task RenameClientScopeAssignmentsAsync(int tenantId, string oldName, string newName, CancellationToken ct);
}
