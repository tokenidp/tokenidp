using IDP.Domain.AggregateRoots.Emails;
using IDP.Infrastructure.Emails.Primitives;
using IDP.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace IDP.Infrastructure.Emails;

internal class EmailQueueStore : IEmailQueueStore
{
    private readonly ApplicationDbContext _db;
    private readonly IAppLogger<EmailQueueStore> _logger;

    public EmailQueueStore(ApplicationDbContext db, IAppLogger<EmailQueueStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CancelPendingAsync(
        int tenantId,
        string messageKey,
        string reason,
        CancellationToken ct)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                EmailSql.CancelPendingByMessageKey,
                new[]
                {
                    new SqlParameter("@tenantId", tenantId),
                    new SqlParameter("@messageKey", messageKey),
                    new SqlParameter("@reason", reason)
                },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel pending emails. TenantId={TenantId}, MessageKey={MessageKey}, Reason={Reason}",
                tenantId,
                messageKey,
                reason);
            throw;
        }
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