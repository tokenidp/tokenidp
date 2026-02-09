using IDP.Domain.AggregateRoots.Emails;

namespace IDP.Foundation.Abstractions;

public interface IEmailQueueStore
{
    Task CancelPendingAsync(
       int tenantId,
       string messageKey,
       string reason,
       CancellationToken ct);

    Task EnqueueAsync(EmailMessage email, CancellationToken ct);

}
