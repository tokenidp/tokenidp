using IDP.Domain.Specifications;

namespace IDP.Domain.DomainEvents.Users;

public sealed record AuthenticationFlowEvent(
        int TenantId,
        long? UserId,
        AuthenticationAction Action,
        AuthenticationResult Result,
        string Description,
        Guid? CorrelationId,
        string? IpAddress,
        string? UserAgent
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}