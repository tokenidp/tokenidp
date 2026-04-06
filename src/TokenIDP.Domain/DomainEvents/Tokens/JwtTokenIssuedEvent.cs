namespace TokenIDP.Domain.DomainEvents.Tokens;

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
}
