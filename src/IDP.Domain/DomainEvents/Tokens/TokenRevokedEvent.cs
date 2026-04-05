namespace IDP.Domain.DomainEvents.Tokens;

public sealed record TokenRevokedEvent(
Guid TokenId,
int TenantId,
long? UserId,
string ClientId,
string SourceType,
string? Reason
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
