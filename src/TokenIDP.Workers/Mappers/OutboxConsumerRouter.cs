using TokenIDP.Infrastructure.Abstractions;

namespace TokenIDP.Workers.Mappers;

public class OutboxConsumerRouter : IOutboxConsumerRouter
{
    public IReadOnlyList<string> ResolveConsumers(IDomainEvent evt)
    {
        return evt switch
        {
            JwtTokenIssuedEvent or
            TokenRevokedEvent or
            TokenRefreshRotatedEvent or
            TokenRefreshReuseDetectedEvent or
            ReferenceTokenIssuedEvent or
            RefreshTokenIssuedEvent or
            TokenExpiredEvent
                => new[]
                {
                    OutboxConsumers.TokenReadModel,
                    OutboxConsumers.Activity
                },

            AuthenticationFlowEvent
                => new[]
                {
                    OutboxConsumers.Activity
                },

            _ => Array.Empty<string>()
        };
    }
}

