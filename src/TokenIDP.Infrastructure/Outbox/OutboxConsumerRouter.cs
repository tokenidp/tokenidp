using TokenIDP.Domain.DomainEvents.Activities;
using TokenIDP.Domain.DomainEvents.Tenants;
using TokenIDP.Domain.DomainEvents.Tokens;
using TokenIDP.Domain.DomainEvents.Users;
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

            ActivityDomainEvent
                => new[]
                {
                    OutboxConsumers.Activity
                },

            TenantCreatedEvent or
            TenantActivatedEvent or
            TenantInactivatedEvent or
            TenantBrandingChangedEvent or
            TenantPlanChangedEvent
                => new[]
                {
                    OutboxConsumers.Activity
                },

            _ => Array.Empty<string>()
        };
    }
}
