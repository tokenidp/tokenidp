using IDP.Domain.ReadModels;

namespace Admin.Core.Activities;

internal sealed class ActivityListItem
{
    public long Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string ActorType { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public string? TargetType { get; private set; }
    public string Target { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;

    public static Expression<Func<Activity, ActivityListItem>> Projection =>
        activity => new ActivityListItem
        {
            Id = activity.Id,
            Timestamp = activity.CreatedOn,
            EventType = activity.EventType.ToString(),
            Category = activity.Category.ToString(),
            Severity = activity.Severity.ToString(),
            ActorType = activity.ActorType.ToString(),
            Actor = !string.IsNullOrWhiteSpace(activity.ActorDisplayName)
                ? activity.ActorDisplayName
                : activity.ActorType.ToString(),
            TargetType = activity.TargetType != null ? activity.TargetType.ToString() : null,
            Target = activity.TargetDescription ?? activity.TargetId ?? (activity.TargetType != null ? activity.TargetType.ToString() : string.Empty),
            Description = activity.Description,
            Status = activity.Status
        };
}