namespace IDP.Domain.AggregateRoots.Emails;

public sealed class EmailDeliveryAttempt : Entity<long>
{          
    public long EmailMessageId { get; private set; }  // aggregate reference
    public int AttemptNo { get; private set; }
    public string? Provider { get; private set; }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }

    public EmailDeliveryOutcome Outcome { get; private set; }

    public string? ProviderMessageId { get; private set; }
    public string? Error { get; private set; }

    private EmailDeliveryAttempt() { } // ORM

    private EmailDeliveryAttempt(
        long emailMessageId,
        int attemptNo,
        string? provider,
        DateTime startedAtUtc)
    {
        if (emailMessageId <= 0) throw new ArgumentOutOfRangeException(nameof(emailMessageId));
        if (attemptNo <= 0) throw new ArgumentOutOfRangeException(nameof(attemptNo));

        EmailMessageId = emailMessageId;
        AttemptNo = attemptNo;
        Provider = provider;
        StartedAtUtc = startedAtUtc;
    }

    public static EmailDeliveryAttempt Start(
        long emailMessageId,
        int attemptNo,
        string? provider,
        DateTime nowUtc)
    {
        return new EmailDeliveryAttempt(emailMessageId, attemptNo, provider, nowUtc);
    }

    public void MarkSuccess(string? providerMessageId, DateTime finishedAtUtc)
    {
        Outcome = EmailDeliveryOutcome.Success;
        ProviderMessageId = providerMessageId;
        FinishedAtUtc = finishedAtUtc;
        Error = null;
    }

    public void MarkTransientFailure(string error, DateTime finishedAtUtc)
    {
        Outcome = EmailDeliveryOutcome.TransientFailure;
        Error = Truncate(error, 2000);
        FinishedAtUtc = finishedAtUtc;
    }

    public void MarkPermanentFailure(string error, DateTime finishedAtUtc)
    {
        Outcome = EmailDeliveryOutcome.PermanentFailure;
        Error = Truncate(error, 2000);
        FinishedAtUtc = finishedAtUtc;
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
}
