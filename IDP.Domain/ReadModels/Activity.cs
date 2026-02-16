using IDP.Domain.ReadModels.Enums;

namespace IDP.Domain.ReadModels;

public sealed class Activity : Entity<long>, ITenant
{
    public long OutboxEventId { get; private set; }
    public int TenantId { get; private set; }
    public ActivityCategory Category { get; private set; }
    public ActivityEventType EventType { get; private set; }
    public ActivitySeverity Severity { get; private set; }

    public ActivityActorType ActorType { get; private set; }
    public string? ActorId { get; private set; }
    public string? ActorDisplayName { get; private set; }

    public ActivityTargetType? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? TargetDescription { get; private set; }

    public string Status { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    public Guid? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private Activity() { }

    public static Activity Create(
    int tenantId,
    ActivityCategory category,
    ActivityEventType eventType,
    ActivitySeverity severity,

    ActivityActorType actorType,
    string? actorId,
    string? actorDisplayName,

    ActivityTargetType? targetType,
    string? targetId,
    string? targetDescription,

    string status,
    string description,

    Guid? correlationId,
    string? ipAddress,
    string? userAgent,

    long outboxEventId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required", nameof(status));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (outboxEventId <= 0)
            throw new ArgumentOutOfRangeException(nameof(outboxEventId));

        return new Activity
        {
            TenantId = tenantId,

            Category = category,
            EventType = eventType,
            Severity = severity,

            ActorType = actorType,
            ActorId = actorId?.Trim(),
            ActorDisplayName = actorDisplayName?.Trim(),

            TargetType = targetType,
            TargetId = targetId?.Trim(),
            TargetDescription = targetDescription?.Trim(),

            Status = status.Trim(),
            Description = description.Trim(),

            CorrelationId = correlationId,
            IpAddress = ipAddress?.Trim(),
            UserAgent = userAgent?.Trim(),

            OutboxEventId = outboxEventId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

