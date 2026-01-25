using IDP.Domain.Specifications;

namespace IDP.Domain.DomainEvents;

public sealed record TokenIssuedDomainEvent(
    Guid TokenId,
    int TenantId,
    long? UserId,
    string ClientId,
    TokenTypes TokenType,
    DateTime ExpiresAt
) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}