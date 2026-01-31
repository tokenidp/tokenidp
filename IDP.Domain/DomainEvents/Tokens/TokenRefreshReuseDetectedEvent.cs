namespace IDP.Domain.DomainEvents.Tokens;

public sealed record TokenRefreshReuseDetectedEvent(
    Guid TokenId,
    int TenantId,
    long UserId,
    string ClientId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}