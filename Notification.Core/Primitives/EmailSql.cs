namespace Notification.Core.Primitives;

public static class EmailSql
{
    public const string CancelPendingByMessageKey = @"
            UPDATE dbo.EmailMessage
            SET
                Status = 4, -- Cancelled
                CancelledAtUtc = SYSUTCDATETIME(),
                CancelReason = @reason
            WHERE
                TenantId = @tenantId
                AND MessageKey = @messageKey
                AND Status = 0; -- Pending only
            ";

    public const string ClaimBatch = @"
            DECLARE @now datetime2(3) = SYSUTCDATETIME();
            DECLARE @lockUntil datetime2(3) = DATEADD(SECOND, @lockSeconds, @now);

            ;WITH cte AS
            (
                SELECT TOP (@batchSize) Id
                FROM dbo.EmailMessage WITH (READPAST, UPDLOCK, ROWLOCK)
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
            FROM dbo.EmailMessage m
            INNER JOIN cte ON cte.Id = m.Id;";
}
