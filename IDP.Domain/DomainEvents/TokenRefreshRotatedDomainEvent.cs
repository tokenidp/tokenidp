namespace IDP.Domain.DomainEvents;

public sealed record TokenRefreshRotatedDomainEvent(
    Guid OldRefreshTokenId,
    Guid NewRefreshTokenId,
    Guid AccessTokenId,
    int TenantId,
    long UserId,
    string ClientId,
    Guid SessionId,
    DateTime RotatedAtUtc
) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
