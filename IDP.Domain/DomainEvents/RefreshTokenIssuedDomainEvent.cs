namespace IDP.Domain.DomainEvents;

public sealed record RefreshTokenIssuedDomainEvent(
    Guid TokenId,
    Guid RefreshTokenId,
    int TenantId,
    long? UserId,
    string ClientId,
    DateTime ExpiresAtUtc,
    string? IpAddress
) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

