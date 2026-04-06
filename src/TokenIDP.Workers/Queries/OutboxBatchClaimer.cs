using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Infrastructure.Persistence;
using System.Data;

namespace TokenIDP.Workers.Queries;

public static class OutboxBatchClaimer
{
    public static async Task<List<long>> ClaimBatchAsync(
        ApplicationDbContext db,
        string consumerName,
        int batchSize,
        TimeSpan lockDuration,
        string workerId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var lockUntil = now.Add(lockDuration);

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var consumers = await db.OutboxEventConsumers
            .Where(x => x.ConsumerName == consumerName
                && x.ProcessedAt == null
                && x.FailedAt == null
                && x.Status == OutboxStatus.Pending
                && (x.NextAttemptAt == null || x.NextAttemptAt <= now)
                && (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var consumer in consumers)
        {
            consumer.Claim(workerId, lockUntil);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return consumers.Select(x => x.OutboxEventId).ToList();
    }
}

