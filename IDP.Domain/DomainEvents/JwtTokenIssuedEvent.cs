using IDP.Domain.Specifications;

namespace IDP.Domain.DomainEvents;

public sealed record JwtTokenIssuedEvent(
    Guid TokenId,
    int TenantId,
    long? UserId,
    string ClientId,
    TokenTypes TokenType,
    DateTime ExpiresAt
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public int TenantId { get; private set; } = TenantId;
    public string AggregateId { get; private set; } = TokenId.ToString();
    public string AggregateType { get; private set; } = "Token";
    public string EventType { get; private set; } = OutboxEventTypes.TokenIssued;
}