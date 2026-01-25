using IDP.Domain.Specifications;

namespace IDP.Domain.DomainEvents;

public sealed record TokenRevokedDomainEvent(
Guid TokenId,
int TenantId,
long? UserId,
string ClientId,
string? Reason
) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
