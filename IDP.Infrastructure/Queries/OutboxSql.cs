namespace IDP.Infrastructure.Queries;

public sealed record ClaimedOutboxRow(long Id);

public static class OutboxSql
{
    public const string ClaimBatch = @"
            DECLARE @now datetime2(3) = SYSUTCDATETIME();

            ;WITH cte AS (
                SELECT TOP (@batchSize) *
                FROM dbo.OutboxEvents WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE ProcessedAt IS NULL
                  AND FailedAt IS NULL
                  AND (NextAttemptAt IS NULL OR NextAttemptAt <= @now)
                  AND (LockedUntil IS NULL OR LockedUntil < @now)
                ORDER BY CreatedAt
            )
            UPDATE cte
            SET LockedUntil = DATEADD(SECOND, @lockSeconds, @now),
                LockedBy = @workerId
            OUTPUT INSERTED.Id;";
}
