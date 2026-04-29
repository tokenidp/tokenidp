using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.DomainEvents.Tokens;

namespace TokenIDP.Infrastructure.Outbox;

internal sealed class TokenOutboxMapper : IOutboxMapper
{
    public bool CanHandle(IDomainEvent evt)
        => evt is JwtTokenIssuedEvent
            or TokenRevokedEvent
            or TokenRefreshRotatedEvent
            or TokenRefreshReuseDetectedEvent
            or ReferenceTokenIssuedEvent
            or RefreshTokenIssuedEvent
            or TokenExpiredEvent;

    public OutboxEvent Map(IDomainEvent evt)
      => evt switch
      {
          TokenExpiredEvent e => Create(e),
          RefreshTokenIssuedEvent e => Create(e),
          ReferenceTokenIssuedEvent e => Create(e),
          TokenRefreshReuseDetectedEvent e => Create(e),
          TokenRefreshRotatedEvent e => Create(e),
          TokenRevokedEvent e => Create(e),
          JwtTokenIssuedEvent e => Create(e),
          _ => throw new InvalidOperationException()
      };

    private OutboxEvent Create(IDomainEvent evt)
    {
        var meta = ResolveMetaData(evt);

        return OutboxEvent.Create(
            tenantId: meta.TenantId,
            eventType: meta.EventType,
            aggregateId: meta.AggregateId!,
            aggregateType: meta.AggregateType,
            payload: evt,
            partitionKey: meta.PartitionKey);
    }

    private OutboxMetadata ResolveMetaData(IDomainEvent evt)
    {
        return evt switch
        {
            TokenExpiredEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(TokenExpiredEvent),
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            RefreshTokenIssuedEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(RefreshTokenIssuedEvent),
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            ReferenceTokenIssuedEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(ReferenceTokenIssuedEvent),
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            TokenRefreshReuseDetectedEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(TokenRefreshReuseDetectedEvent),
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            TokenRefreshRotatedEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(TokenRefreshRotatedEvent),
                AggregateType: "Token",
                AggregateId: e.NewRefreshTokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            TokenRevokedEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(TokenRevokedEvent),
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            JwtTokenIssuedEvent e => new OutboxMetadata(
                TenantId: e.TenantId,
                EventType: nameof(JwtTokenIssuedEvent),
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"),
            _ => throw new InvalidOperationException(
                $"No outbox mapping defined for {evt.GetType().Name}")
        };
    }
}
