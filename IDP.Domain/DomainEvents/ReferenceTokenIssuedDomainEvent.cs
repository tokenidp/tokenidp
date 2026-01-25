namespace IDP.Domain.DomainEvents;

public sealed record ReferenceTokenIssuedDomainEvent(
    Guid TokenId,
    Guid ReferenceTokenId,
    int TenantId,
    long? UserId,
    string ClientId,
    DateTime ExpiresAtUtc
) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

