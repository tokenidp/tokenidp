using TokenIDP.Domain.Base;
using TokenIDP.Domain.DomainEvents.Tokens;
using TokenIDP.Domain.DomainEvents.Users;
using TokenIDP.Domain.ReadModels;
using TokenIDP.Infrastructure.Outbox.Abstractions;

namespace TokenIDP.Infrastructure.Outbox;

public sealed class OutboxConsumerRouter : IOutboxConsumerRouter
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
