using TokenIDP.Domain.AggregateRoots.Emails;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IEmailQueueRepository
{
    Task CancelPendingAsync(
       int tenantId,
       string messageKey,
       string reason,
       CancellationToken ct);

    Task EnqueueAsync(EmailMessage email, CancellationToken ct);
}

