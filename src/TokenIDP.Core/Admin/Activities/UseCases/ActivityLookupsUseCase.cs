using TokenIDP.Core.Abstractions;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Core.Admin.Activities.UseCases;

internal sealed class ActivityLookupsUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ActivityLookupsUseCase> _logger;

    public ActivityLookupsUseCase(
        ICurrentUserService currentUserService,
        IAppLogger<ActivityLookupsUseCase> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public Task<ApiResult<ActivityLookups>> GetLookups(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching activity lookups for tenant {TenantId}", _currentUserService.TenantId);

        var eventTypes = Enum.GetValues<ActivityEventType>()
            .OrderBy(value => (int)value)
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            })
            .ToList();

        var actorTypes = Enum.GetValues<ActivityActorType>()
            .OrderBy(value => (int)value)
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            })
            .ToList();

        return Task.FromResult(ApiResult<ActivityLookups>.Success(new ActivityLookups
        {
            EventTypes = eventTypes,
            ActorTypes = actorTypes
        }));
    }
}

