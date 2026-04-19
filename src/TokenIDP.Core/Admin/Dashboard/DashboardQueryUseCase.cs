using TokenIDP.Core.Admin.Tokens.UseCases;
using TokenIDP.Domain.ReadModels;
using TokenIDP.Domain.ReadModels.Enums;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Queries;

namespace TokenIDP.Core.Admin.Dashboard;

internal sealed class DashboardQueryUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDashboardReadService _dashboardReadService;
    private readonly IAppLogger<DashboardQueryUseCase> _logger;

    public DashboardQueryUseCase(
        IDashboardReadService dashboardReadService,
        ICurrentUserService currentUserService,
        IAppLogger<DashboardQueryUseCase> logger)
    {
        _dashboardReadService = dashboardReadService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<DashboardResponse>> GetDashboard(string? period, CancellationToken ct)
    {
        var selectedPeriod = DashboardPeriodExtensions.Parse(period);
        var dashboard = await _dashboardReadService.GetDashboardAsync(_currentUserService.TenantId, selectedPeriod, ct);
        return ApiResult<DashboardResponse>.Success(dashboard);
    }
}


