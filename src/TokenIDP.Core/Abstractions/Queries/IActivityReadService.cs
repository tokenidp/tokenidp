using TokenIDP.Core.Admin.Activities;
using TokenIDP.Core.Admin.Common;

namespace TokenIDP.Core.Abstractions.Queries;

public interface IActivityReadService
{
    Task<PaginatedList<ActivityListItem>> SearchActivitiesAsync(int tenantId, SearchData request, CancellationToken ct);
}
