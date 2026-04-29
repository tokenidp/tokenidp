using TokenIDP.Core.Abstractions.Queries;
using TokenIDP.Core.Admin.Activities;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class ActivityReadService : IActivityReadService
{
    private readonly ApplicationDbContext _db;

    public ActivityReadService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PaginatedList<ActivityListItem>> SearchActivitiesAsync(int tenantId, SearchData request, CancellationToken ct)
    {
        var query = _db.Activities
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            if (term.Length >= 3)
            {
                query = query.Where(activity =>
                    (activity.ActorDisplayName ?? string.Empty).ToLower().Contains(term) ||
                    (activity.ActorId ?? string.Empty).ToLower().Contains(term) ||
                    (activity.TargetDescription ?? string.Empty).ToLower().Contains(term) ||
                    (activity.TargetId ?? string.Empty).ToLower().Contains(term) ||
                    (activity.Description ?? string.Empty).ToLower().Contains(term) ||
                    (activity.Status ?? string.Empty).ToLower().Contains(term) ||
                    activity.EventType.ToString().ToLower().Contains(term) ||
                    activity.Category.ToString().ToLower().Contains(term));
            }
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var startDateCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "StartDate", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(startDateCriteria?.Value) &&
            DateTime.TryParse(startDateCriteria.Value, out var startDate))
        {
            query = query.Where(activity => activity.CreatedAtUtc >= startDate.Date);
        }

        var endDateCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "EndDate", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(endDateCriteria?.Value) &&
            DateTime.TryParse(endDateCriteria.Value, out var endDate))
        {
            var inclusiveEnd = endDate.Date.AddDays(1).AddTicks(-1);
            query = query.Where(activity => activity.CreatedAtUtc <= inclusiveEnd);
        }

        criterias = criterias
            .Where(c =>
                !string.Equals(c.ColumnName, "StartDate", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(c.ColumnName, "EndDate", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var eventTypeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "EventType", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(eventTypeCriteria?.Value) &&
            Enum.TryParse<ActivityEventType>(eventTypeCriteria.Value, true, out var eventType))
        {
            query = query.Where(activity => activity.EventType == eventType);
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "EventType", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var actorTypeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "ActorType", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(actorTypeCriteria?.Value) &&
            Enum.TryParse<ActivityActorType>(actorTypeCriteria.Value, true, out var actorType))
        {
            query = query.Where(activity => activity.ActorType == actorType);
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "ActorType", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var categoryCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Category", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(categoryCriteria?.Value) &&
            Enum.TryParse<ActivityCategory>(categoryCriteria.Value, true, out var category))
        {
            query = query.Where(activity => activity.Category == category);
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Category", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(statusCriteria?.Value))
        {
            var status = statusCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(activity => activity.Status.ToLower() == status);
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Select(ActivityListItem.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }
}
