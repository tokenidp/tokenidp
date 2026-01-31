using IDP.Domain.Specifications;

namespace IDP.Domain.DomainEvents.Tokens;

public sealed record RefreshTokenIssuedEvent(
    Guid TokenId,
    int TenantId,
    long? UserId,
    string ClientId,
    TokenTypes TokenType,
    DateTime ExpiresAt,
    string? IpAddress
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

