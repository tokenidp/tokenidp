namespace IDP.Projection.Queries;

public static class EmailSql
{
    public const string ClaimBatch = @"
            DECLARE @now datetime2(3) = SYSUTCDATETIME();
            DECLARE @lockUntil datetime2(3) = DATEADD(SECOND, @lockSeconds, @now);

            ;WITH cte AS
            (
                SELECT TOP (@batchSize) Id
                FROM dbo.EmailMessages WITH (READPAST, UPDLOCK, ROWLOCK)
                WHERE
                    Status = 0
                    AND (ScheduledAtUtc IS NULL OR ScheduledAtUtc <= @now)
                    AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc <= @now)
                    AND (LockedUntilUtc IS NULL OR LockedUntilUtc < @now)
                ORDER BY Priority ASC, CreatedAtUtc ASC
            )
            UPDATE m
            SET
                Status = 1,
                LockedBy = @workerId,
                LockedUntilUtc = @lockUntil
            OUTPUT inserted.Id
            FROM dbo.EmailMessages m
            INNER JOIN cte ON cte.Id = m.Id;";
}
