using TokenIDP.Domain.AggregateRoots.Emails.ValueObjects;

namespace TokenIDP.Domain.AggregateRoots.Emails;

public enum EmailPayloadMode : byte
{
    RenderedBodies = 0, //rendered the email at enqueue time, Worker just sends it, No template lookup needed
    TemplateRef = 1, //The email only stores a reference to a template + model: EmailMessage.TemplateKey, EmailMessage.TemplateModelJson
    Hybrid = 2 //Template reference, AND rendered bodies - If rendered bodies already exist (Hybrid), you can decide to skip re-render.
}

public enum EmailStatus : byte
{
    Pending = 0,
    Claimed = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}

public sealed class EmailMessage : AggregateRoot<long>, ITenant
{
    public int TenantId { get; private set; }
    public string MessageKey { get; private set; } = string.Empty;

    public EmailStatus Status { get; private set; }
    public byte Priority { get; private set; }
    public EmailPayloadMode PayloadMode { get; private set; }
    public string ToAddress { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }

    public string? Provider { get; private set; }

    public string? FromAddress { get; private set; }
    public string? FromName { get; private set; }

    public string? Subject { get; private set; }
    public string? BodyHtml { get; private set; }
    public string? BodyText { get; private set; }

    public string? TemplateKey { get; private set; }
    public string? TemplateModelJson { get; private set; }

    public Guid? CorrelationId { get; private set; }
    public string? Tags { get; private set; }

    public DateTime? ScheduledAtUtc { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }

    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }

    public string? LockedBy { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }
    public string? ProviderMessageId { get; private set; }

    public string? LastError { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }

    private EmailMessage() { } // EF

    public static EmailMessage CreateTemplate(
        int tenantId,
        string messageKey,
        ValueObjects.EmailRecipient recipient,
        EmailTemplateRef template,
        string? provider = null,
        string? fromAddress = null,
        string? fromName = null,
        byte priority = 5,
        int maxAttempts = 10,
        DateTime? scheduledAtUtc = null,
        Guid? correlationId = null,
        string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(messageKey)) throw new ArgumentException("MessageKey is required.");
        if (string.IsNullOrWhiteSpace(template.TemplateKey)) throw new ArgumentException("TemplateKey is required.");

        var msg = new EmailMessage
        {

            TenantId = tenantId,
            MessageKey = messageKey.Trim(),
            Status = EmailStatus.Pending,
            Priority = priority,
            PayloadMode = EmailPayloadMode.TemplateRef,
            Provider = provider,
            FromAddress = fromAddress,
            FromName = fromName,
            TemplateKey = template.TemplateKey.Trim(),
            TemplateModelJson = template.ModelJson,
            ScheduledAtUtc = scheduledAtUtc,
            MaxAttempts = maxAttempts,
            CorrelationId = correlationId,
            Tags = tags
        };

        msg.AddRecipients(recipient);
        msg.ValidatePayload();
        return msg;
    }

    public static EmailMessage CreateRendered(
        int tenantId,
        string messageKey,
        ValueObjects.EmailRecipient recipient,
        string subject,
        string? bodyHtml,
        string? bodyText,
        string? provider = null,
        string? fromAddress = null,
        string? fromName = null,
        byte priority = 5,
        int maxAttempts = 10,
        DateTime? scheduledAtUtc = null,
        Guid? correlationId = null,
        string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(messageKey)) throw new ArgumentException("MessageKey is required.");

        var msg = new EmailMessage
        {
            TenantId = tenantId,
            MessageKey = messageKey.Trim(),
            Status = EmailStatus.Pending,
            Priority = priority,
            PayloadMode = EmailPayloadMode.RenderedBodies,
            Provider = provider,
            FromAddress = fromAddress,
            FromName = fromName,
            Subject = subject,
            BodyHtml = bodyHtml,
            BodyText = bodyText,
            ScheduledAtUtc = scheduledAtUtc,
            MaxAttempts = maxAttempts,
            CorrelationId = correlationId,
            Tags = tags
        };

        msg.AddRecipients(recipient);
        msg.ValidatePayload();
        return msg;
    }

    public void AddRecipients(ValueObjects.EmailRecipient recipient)
    {
        if (recipient == null) throw new InvalidOperationException("At least one recipient is required.");

        ToAddress = recipient.Address.Value;
        DisplayName = recipient.DisplayName;
    }

    public void Cancel(string reason)
    {
        if (Status is EmailStatus.Sent or EmailStatus.Failed) return; // or throw if you prefer
        Status = EmailStatus.Cancelled;
        CancelReason = (reason ?? "Cancelled").Trim();
        CancelledAtUtc = DateTime.UtcNow;
        ClearLock();
    }

    public void Claim(string workerId, DateTime lockUntilUtc)
    {
        if (Status != EmailStatus.Pending)
            throw new InvalidOperationException($"Email must be pending to claim. Current: {Status}");

        Status = EmailStatus.Claimed;
        LockedBy = workerId;
        LockedUntilUtc = lockUntilUtc;
    }

    public void MarkSent(string providerMessageId)
    {
        EnsureClaimed();
        Status = EmailStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        ProviderMessageId = providerMessageId;
        LastError = null;
        ClearLock();
    }

    public void MarkTransientFailure(string error, DateTime nextAttemptAtUtc)
    {
        EnsureClaimed();

        AttemptCount++;
        LastError = Truncate(error, 2000);

        if (AttemptCount >= MaxAttempts)
        {
            Status = EmailStatus.Failed;
            FailedAtUtc = DateTime.UtcNow;
            ClearLock();
            return;
        }

        Status = EmailStatus.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc;
        ClearLock();
    }

    public void MarkPermanentFailure(string error)
    {
        EnsureClaimed();
        AttemptCount++;
        LastError = Truncate(error, 2000);
        Status = EmailStatus.Failed;
        FailedAtUtc = DateTime.UtcNow;
        ClearLock();
    }

    public void ApplyRenderedBodies(string subject, string? html, string? text)
    {
        // used when PayloadMode is TemplateRef and worker renders bodies
        Subject = subject;
        BodyHtml = html;
        BodyText = text;
        if (PayloadMode == EmailPayloadMode.TemplateRef)
            PayloadMode = EmailPayloadMode.Hybrid;
    }

    private void EnsureClaimed()
    {
        if (Status != EmailStatus.Claimed)
            throw new InvalidOperationException($"Email must be Claimed. Current: {Status}");
    }

    private void ClearLock()
    {
        LockedBy = null;
        LockedUntilUtc = null;
    }

    private void ValidatePayload()
    {
        if (string.IsNullOrEmpty(ToAddress)) throw new InvalidOperationException("Recipients required.");

        switch (PayloadMode)
        {
            case EmailPayloadMode.RenderedBodies:
                if (string.IsNullOrWhiteSpace(Subject)) throw new InvalidOperationException("Subject required.");
                if (string.IsNullOrWhiteSpace(BodyHtml) && string.IsNullOrWhiteSpace(BodyText))
                    throw new InvalidOperationException("BodyHtml or BodyText required.");
                break;

            case EmailPayloadMode.TemplateRef:
                if (string.IsNullOrWhiteSpace(TemplateKey)) throw new InvalidOperationException("TemplateKey required.");
                break;

            case EmailPayloadMode.Hybrid:
                // allow both
                break;
        }
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
}

