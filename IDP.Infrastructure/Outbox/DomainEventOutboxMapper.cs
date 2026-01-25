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
            ResolveTenant(domainEvent),
            ResolveAggregateId(domainEvent),
            ResolvePartitionKey(domainEvent)
        );
    }

    private static int ResolveTenant(IDomainEvent evt)
    {
        return evt switch
        {
            TokenIssuedDomainEvent e => e.TenantId,
            TokenRevokedDomainEvent e => e.TenantId,
            TokenRefreshRotatedDomainEvent e => e.TenantId,
            TokenRefreshReuseDetectedDomainEvent e => e.TenantId,
            ReferenceTokenIssuedDomainEvent e => e.TenantId,
            RefreshTokenIssuedDomainEvent e => e.TenantId,
            _ => 0
        };
    }

    private static long? ResolveAggregateId(IDomainEvent evt)
    {
        return evt switch
        {
            TokenIssuedDomainEvent e => e.UserId,
            TokenRevokedDomainEvent e => e.UserId,
            TokenRefreshRotatedDomainEvent e => e.UserId,
            TokenRefreshReuseDetectedDomainEvent e => e.UserId,
            ReferenceTokenIssuedDomainEvent e => e.UserId,
            RefreshTokenIssuedDomainEvent e => e.UserId,
            _ => null
        };
    }

    private static string? ResolvePartitionKey(IDomainEvent evt)
    {
        return evt switch
        {
            TokenIssuedDomainEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            TokenRevokedDomainEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            TokenRefreshRotatedDomainEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            TokenRefreshReuseDetectedDomainEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            ReferenceTokenIssuedDomainEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            RefreshTokenIssuedDomainEvent e => $"tenant:{e.TenantId}:user:{e.UserId}",
            _ => null
        };
    }
}