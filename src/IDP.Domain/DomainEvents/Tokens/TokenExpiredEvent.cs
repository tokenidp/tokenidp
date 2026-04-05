namespace IDP.Domain.DomainEvents.Tokens;

public sealed record TokenExpiredEvent(
Guid TokenId,
int TenantId,
long? UserId,
string ClientId,
string SourceType
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}