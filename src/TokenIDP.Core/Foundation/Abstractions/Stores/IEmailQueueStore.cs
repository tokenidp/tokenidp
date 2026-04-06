using TokenIDP.Domain.AggregateRoots.Emails;

namespace TokenIDP.Core.Foundation.Abstractions.Stores;

public interface IEmailQueueStore
{
    Task CancelPendingAsync(
       int tenantId,
       string messageKey,
       string reason,
       CancellationToken ct);

    Task EnqueueAsync(EmailMessage email, CancellationToken ct);
}

