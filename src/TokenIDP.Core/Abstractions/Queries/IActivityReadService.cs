using TokenIDP.Core.Admin.Activities;

namespace TokenIDP.Core.Abstractions.Queries;

public interface IActivityReadService
{
    Task<PaginatedList<ActivityListItem>> SearchActivitiesAsync(int tenantId, SearchData request, CancellationToken ct);
}
