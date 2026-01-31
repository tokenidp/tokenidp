namespace IDP.Projection.Mappers;

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

        return OutboxEvent.Create
            (
                 tenantId: meta.TenantId,
                 eventType: evt.GetType().Name,
                 aggregateId: meta.AggregateId!,
                 aggregateType: meta.AggregateType,
                 payload: evt,
                 partitionKey: meta.PartitionKey
            );
    }

    private OutboxMetadata ResolveMetaData(IDomainEvent evt)
    {
        return evt switch
        {
            TokenExpiredEvent e => new OutboxMetadata
            (
                TenantId: e.TenantId,
                EventType: OutboxEventTypes.TokenExpired,
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            RefreshTokenIssuedEvent e => new OutboxMetadata
            (
                TenantId: e.TenantId,
                EventType: OutboxEventTypes.RefreshTokenIssued,
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            ReferenceTokenIssuedEvent e => new OutboxMetadata
            (
                TenantId: e.TenantId,
                EventType: OutboxEventTypes.RefreshTokenIssued,
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            TokenRefreshReuseDetectedEvent e => new OutboxMetadata
            (
               TenantId: e.TenantId,
               EventType: OutboxEventTypes.TokenRefreshReuseDetected,
               AggregateType: "Token",
               AggregateId: e.TokenId.ToString(),
               PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            TokenRefreshRotatedEvent e => new OutboxMetadata
            (
                TenantId: e.TenantId,
                EventType: OutboxEventTypes.TokenRefreshRotated,
                AggregateType: "Token",
                AggregateId: e.NewRefreshTokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            TokenRevokedEvent e => new OutboxMetadata
            (
               TenantId: e.TenantId,
               EventType: OutboxEventTypes.TokenRevoked,
               AggregateType: "Token",
               AggregateId: e.TokenId.ToString(),
               PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            JwtTokenIssuedEvent e => new OutboxMetadata
            (
                TenantId: e.TenantId,
                EventType: OutboxEventTypes.TokenIssued,
                AggregateType: "Token",
                AggregateId: e.TokenId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:token"
            ),
            _ => throw new InvalidOperationException(
               $"No outbox mapping defined for {evt.GetType().Name}")
        };
    }
}
