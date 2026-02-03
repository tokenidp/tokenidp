namespace IDP.Projection.Queries;

public sealed record ClaimedOutboxRow(long Id);

public static class OutboxSql
{
    public const string ClaimBatch = @"
            DECLARE @now datetime2(3) = SYSUTCDATETIME();

            ;WITH cte AS (

                SELECT TOP (@batchSize) *
                FROM OutboxEventConsumers WITH (UPDLOCK, READPAST)
                WHERE ConsumerName = @consumer
                AND ProcessedAt IS NULL AND FailedAt IS NULL
                AND Status = 0
                AND (NextAttemptAt IS NULL OR NextAttemptAt <= @now)
                AND (LockedUntil IS NULL OR LockedUntil < @now)
                ORDER BY Id
            )
            UPDATE cte
            SET Status = 1, LockedUntil = DATEADD(SECOND, @lockSeconds, @now),
                LockedBy = @workerId
            OUTPUT INSERTED.OutboxEventId;";
}
