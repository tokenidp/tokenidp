using TokenIDP.Domain.DomainEvents.Activities;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Workers.Workers;

internal static class BackgroundJobActivityEvents
{
    public static void RaiseFailure(
        ApplicationDbContext db,
        string jobName,
        string workerId,
        Exception exception,
        Guid? correlationId = null,
        int tenantId = 0,
        string? targetId = null)
    {
        db.AddDomainEvent(new ActivityDomainEvent(
            TenantId: tenantId,
            EventType: ActivityEventType.BackgroundJobFailed,
            AggregateType: "BackgroundJob",
            AggregateId: targetId ?? workerId,
            ActorId: null,
            ActorDisplayName: null,
            TargetId: targetId ?? workerId,
            TargetDescription: jobName,
            Status: "Failed",
            Description: $"{jobName} failed: {exception.Message}",
            CorrelationId: correlationId));
    }
}
