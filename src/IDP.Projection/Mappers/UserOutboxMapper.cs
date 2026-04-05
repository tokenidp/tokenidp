using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Mappers;

internal sealed class UserOutboxMapper : IOutboxMapper
{
    public bool CanHandle(IDomainEvent evt)
        => evt is AuthenticationFlowEvent;

    public OutboxEvent Map(IDomainEvent evt)
      => evt switch
      {
          AuthenticationFlowEvent e => Create(e),
          _ => throw new InvalidOperationException()
      };

    private OutboxEvent Create(IDomainEvent evt)
    {
        var meta = ResolveMetaData(evt);

        return OutboxEvent.Create
            (
                 tenantId: meta.TenantId,
                 eventType: meta.EventType,
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
            AuthenticationFlowEvent e => new OutboxMetadata
            (
                TenantId: e.TenantId,
                EventType: ResolveEventType(e).ToString(),
                AggregateType: "User",
                AggregateId: e.UserId.ToString(),
                PartitionKey: $"tenant:{e.TenantId}:user"
            ),
            _ => throw new InvalidOperationException(
               $"No outbox mapping defined for {evt.GetType().Name}")
        };
    }

    private ActivityEventType ResolveEventType(IDomainEvent evt)
    {
        return evt switch
        {
            AuthenticationFlowEvent e => e switch
            {
                { Action: AuthenticationAction.Login, Result: AuthenticationResult.Success }
                    => ActivityEventType.LoginSucceeded,

                { Action: AuthenticationAction.Login, Result: AuthenticationResult.Failed }
                    => ActivityEventType.LoginFailed,

                { Action: AuthenticationAction.Logout }
                    => ActivityEventType.Logout,

                { Action: AuthenticationAction.MfaChallenge, Result: AuthenticationResult.Requested }
                    => ActivityEventType.MfaChallengeSent,

                { Action: AuthenticationAction.MfaChallenge, Result: AuthenticationResult.Success }
                    => ActivityEventType.MfaValidated,

                { Action: AuthenticationAction.MfaChallenge, Result: AuthenticationResult.Failed }
                    => ActivityEventType.MfaFailed,

                { Action: AuthenticationAction.PasswordReset, Result: AuthenticationResult.Requested }
                    => ActivityEventType.PasswordResetRequested,

                { Action: AuthenticationAction.PasswordReset, Result: AuthenticationResult.Completed }
                    => ActivityEventType.PasswordResetCompleted,

                { Result: AuthenticationResult.Locked }
                    => ActivityEventType.AccountLocked,

                { Result: AuthenticationResult.Unlocked }
                    => ActivityEventType.AccountUnlocked,

                _ => throw new InvalidOperationException(
                    $"Unsupported authentication flow: {e.Action}/{e.Result}")
            },

            _ => throw new InvalidOperationException(
                $"No activity mapping defined for {evt.GetType().Name}")
        };
    }
}