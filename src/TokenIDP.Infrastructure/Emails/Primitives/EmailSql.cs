using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Emails.Primitives;

public static class EmailSql
{
    public static async Task CancelPendingByMessageKeyAsync(
        ApplicationDbContext db,
        int tenantId,
        string messageKey,
        string reason,
        CancellationToken ct)
    {
        var pendingEmails = await db.EmailMessages
            .Where(x => x.TenantId == tenantId
                && x.MessageKey == messageKey
                && x.Status == EmailStatus.Pending)
            .ToListAsync(ct);

        foreach (var pendingEmail in pendingEmails)
        {
            pendingEmail.Cancel(reason);
        }

        await db.SaveChangesAsync(ct);
    }
}

