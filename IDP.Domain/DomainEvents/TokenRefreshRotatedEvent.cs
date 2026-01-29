using IDP.Domain.Specifications;

namespace IDP.Domain.DomainEvents;

public sealed record TokenRefreshRotatedEvent(
    Guid OldRefreshTokenId,
    Guid NewRefreshTokenId,
    int TenantId,
    long UserId,
    string ClientId,
    DateTime RotatedAt
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public int TenantId { get; private set; } = TenantId;
    public string AggregateId { get; private set; } = NewRefreshTokenId.ToString();
    public string AggregateType { get; private set; } = "Token";
    public string EventType { get; private set; } = OutboxEventTypes.TokenRefreshRotated;
}
