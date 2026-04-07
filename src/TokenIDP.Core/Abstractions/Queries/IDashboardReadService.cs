using TokenIDP.Core.Admin.Dashboard;

namespace TokenIDP.Core.Abstractions.Queries;

public interface IDashboardReadService
{
    Task<DashboardResponse> GetDashboardAsync(int tenantId, CancellationToken ct);
}
