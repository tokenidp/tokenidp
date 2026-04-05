namespace IDP.Domain.DomainEvents.Tokens;

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
}
