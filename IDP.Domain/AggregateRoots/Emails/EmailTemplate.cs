namespace IDP.Domain.AggregateRoots.Emails;

public sealed class EmailTemplate : AggregateRoot<long>, ITenant
{           
    public int TenantId { get; private set; }
    public string TemplateKey { get; private set; } = string.Empty;

    public string SubjectTemplate { get; private set; } = string.Empty;
    public string? HtmlTemplate { get; private set; }
    public string? TextTemplate { get; private set; }

    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    private EmailTemplate() { } // for ORM

    public EmailTemplate(
        int tenantId,
        string templateKey,
        string subjectTemplate,
        string? htmlTemplate,
        string? textTemplate,
        bool isActive = true)
    {
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(templateKey)) throw new ArgumentException("TemplateKey is required.");
        if (string.IsNullOrWhiteSpace(subjectTemplate)) throw new ArgumentException("Subject template is required.");
        if (string.IsNullOrWhiteSpace(htmlTemplate) && string.IsNullOrWhiteSpace(textTemplate))
            throw new ArgumentException("Either HtmlTemplate or TextTemplate must be provided.");

        TenantId = tenantId;
        TemplateKey = templateKey.Trim();
        SubjectTemplate = subjectTemplate;
        HtmlTemplate = htmlTemplate;
        TextTemplate = textTemplate;
        IsActive = isActive;
        Version = 1;
    }

    public void UpdateContent(string subjectTemplate, string? htmlTemplate, string? textTemplate)
    {
        if (string.IsNullOrWhiteSpace(subjectTemplate))
            throw new ArgumentException("Subject template is required.");
        if (string.IsNullOrWhiteSpace(htmlTemplate) && string.IsNullOrWhiteSpace(textTemplate))
            throw new ArgumentException("Either HtmlTemplate or TextTemplate must be provided.");

        SubjectTemplate = subjectTemplate;
        HtmlTemplate = htmlTemplate;
        TextTemplate = textTemplate;
        Version++;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void RenameKey(string newKey)
    {
        if (string.IsNullOrWhiteSpace(newKey))
            throw new ArgumentException("TemplateKey is required.");

        TemplateKey = newKey.Trim();
        Version++;
    }

    public void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Email template '{TemplateKey}' is inactive.");
    }
}
