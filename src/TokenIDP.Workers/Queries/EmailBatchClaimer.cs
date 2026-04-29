using System.Data;
using TokenIDP.Domain.AggregateRoots.Emails;

namespace TokenIDP.Workers.Queries;

public static class EmailBatchClaimer
{
    public static async Task<List<long>> ClaimBatchAsync(
        ApplicationDbContext db,
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

        var messages = await db.EmailMessages
            .Where(x => x.Status == EmailStatus.Pending
                && (x.ScheduledAtUtc == null || x.ScheduledAtUtc <= now)
                && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now)
                && (x.LockedUntilUtc == null || x.LockedUntilUtc < now))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            message.Claim(workerId, lockUntil);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return messages.Select(x => x.Id).ToList();
    }
}

