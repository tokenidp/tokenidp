namespace IDP.Infrastructure.Emails.Primitives;

public static class EmailSql
{
    public const string CancelPendingByMessageKey = @"
            UPDATE dbo.EmailMessages
            SET
                Status = 4, -- Cancelled
                CancelledAtUtc = SYSUTCDATETIME(),
                CancelReason = @reason
            WHERE
                TenantId = @tenantId
                AND MessageKey = @messageKey
                AND Status = 0; -- Pending only
            ";
}
