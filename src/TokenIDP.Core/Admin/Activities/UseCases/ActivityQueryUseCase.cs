using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.ReadModels.Enums;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Queries;

namespace TokenIDP.Core.Admin.Activities.UseCases;

internal sealed class ActivityQueryUseCase
{
    private readonly IActivityReadService _activityReadService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ActivityQueryUseCase> _logger;

    public ActivityQueryUseCase(
        IActivityReadService activityReadService,
        ICurrentUserService currentUserService,
        IAppLogger<ActivityQueryUseCase> logger)
    {
        _activityReadService = activityReadService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<PaginatedList<ActivityListItem>>> GetActivities(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching activities list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var activities = await _activityReadService.SearchActivitiesAsync(
            _currentUserService.TenantId,
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} activities", activities.TotalCount);

        return ApiResult<PaginatedList<ActivityListItem>>.Success(activities);
    }
}
