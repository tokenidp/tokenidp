using IDP.Domain.AggregateRoots.Emails;
using IDP.Infrastructure.Emails.Primitives;
using IDP.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace IDP.Infrastructure.Emails;

internal class EmailQueueStore : IEmailQueueStore
{
    private readonly ApplicationDbContext _db;

    public EmailQueueStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task CancelPendingAsync(
        int tenantId,
        string messageKey,
        string reason,
        CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync(
            EmailSql.CancelPendingByMessageKey,
            new SqlParameter("@tenantId", tenantId),
            new SqlParameter("@messageKey", messageKey),
            new SqlParameter("@reason", reason),
            ct);
    }

    public async Task EnqueueAsync(EmailMessage email, CancellationToken ct)
    {
        var cancelPrevious = EmailQueuePolicies.ShouldCancelPrevious(email.MessageKey);

        if (cancelPrevious)
        {
            await CancelPendingAsync(email.TenantId, email.MessageKey, "Superseded", ct);
        }

        _db.EmailMessages.Add(email);

        await _db.SaveChangesAsync(ct);
    }
}

public static class EmailQueuePolicies
{
    public static bool ShouldCancelPrevious(string messageKey)
        => messageKey.StartsWith("mfa:")
        || messageKey.StartsWith("password-reset:");
}