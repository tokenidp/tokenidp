using IDP.Domain.AggregateRoots;
using IDP.Domain.DomainEvents;

namespace IDP.Infrastructure.Outbox;

public static class DomainEventOutboxMapper
{
    public static OutboxEvent Map(
        IDomainEvent domainEvent)
    {
        return OutboxEventFactory.CreateFromDomainEvent(
            domainEvent,
            ResolvePartitionKey(domainEvent)
        );
    }

    private static string? ResolvePartitionKey(IDomainEvent evt)
    {
        return evt switch
        {
            JwtTokenIssuedEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            TokenRevokedEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            TokenRefreshRotatedEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            TokenRefreshReuseDetectedEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            ReferenceTokenIssuedEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            RefreshTokenIssuedEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            _ => null
        };
    }
}