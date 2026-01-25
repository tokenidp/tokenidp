namespace IDP.Domain.DomainEvents;

public sealed record TokenRefreshReuseDetectedDomainEvent(
    int TenantId,
    long UserId,
    string ClientId,
    Guid RefreshTokenId
) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}