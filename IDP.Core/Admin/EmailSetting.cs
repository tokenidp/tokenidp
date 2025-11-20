using IDP.Common.Notifications;

namespace IDP.Core.Application;

internal class EmailSetting : IEmailSetting
{
    private readonly LookupRepo _lookupService;
    private IEnumerable<LookupValue>? _settings;

    public EmailSetting(LookupRepo lookupService)
    {
        _lookupService = lookupService;
    }

    public string? ApiKey { get; private set; }
    public string? SmtpServer { get; private set; }
    public int SmtpPort { get; private set; }
    public string? SmtpUsername { get; private set; }
    public string? SmtpPassword { get; private set; }
    public bool SmtpUseSsl { get; private set; }
    public string? FromEmail { get; private set; }
    public string? FromName { get; private set; }
    public int RetryAttempts { get; private set; }
    public int RetryDelay { get; private set; }
    public EmailProviderType EmailProviderType { get; private set; }

    public async Task PopulateEmailSettings(int tenantId)
    {
        _settings = await _lookupService
            .GeTenantLookupsByType(tenantId, "EmailSettings");

        SetApiKey();
        SetSmtpServer();
        SetSmtpPort();
        SetSmtpUsername();
        SetSmtpPassword();
        SetSmtpUseSsl();
        SetFromEmail();
        SetFromName();
        SetRetryAttempts();
        SetRetryDelay();
        SetEmailProviderType();
    }

    private void SetApiKey()
    {
        var apikey = _settings?.Where(l => l.LookupCode == "ApiKey")
            .Select(s => s.Value).FirstOrDefault();

        ApiKey = apikey;
    }

    private void SetSmtpServer()
    {
        var smtpServer = _settings?.Where(l => l.LookupCode == "SmtpServer")
            .Select(s => s.Value).FirstOrDefault();

        SmtpServer = smtpServer;
    }

    private void SetSmtpPort()
    {
        var smtpPort = _settings?.Where(l => l.LookupCode == "SmtpPort")
            .Select(s => Convert.ToInt32(s.Value)).FirstOrDefault();

        SmtpPort = smtpPort ?? 0;
    }

    private void SetSmtpUsername()
    {
        var smtpUsername = _settings?.Where(l => l.LookupCode == "SmtpUsername")
            .Select(s => s.Value).FirstOrDefault();

        SmtpUsername = smtpUsername;
    }

    private void SetSmtpPassword()
    {
        var smtpPassword = _settings?.Where(l => l.LookupCode == "SmtpPassword")
            .Select(s => s.Value).FirstOrDefault();

        SmtpPassword = smtpPassword;
    }

    private void SetSmtpUseSsl()
    {
        var smtpUseSsl = _settings?.Where(l => l.LookupCode == "SmtpUseSsl")
            .Select(s => Convert.ToBoolean(s.Value)).FirstOrDefault();

        SmtpUseSsl = smtpUseSsl ?? true;
    }

    private void SetFromEmail()
    {
        var fromEmail = _settings?.Where(l => l.LookupCode == "FromEmail")
            .Select(s => s.Value).FirstOrDefault();

        FromEmail = fromEmail;
    }

    private void SetFromName()
    {
        var fromName = _settings?.Where(l => l.LookupCode == "FromName")
            .Select(s => s.Value).FirstOrDefault();

        FromName = fromName;
    }

    private void SetRetryAttempts()
    {
        var retryAttempts = _settings?.Where(l => l.LookupCode == "RetryAttempts")
            .Select(s => Convert.ToInt32(s.Value)).FirstOrDefault();

        RetryAttempts = retryAttempts ?? 3;
    }

    private void SetRetryDelay()
    {
        var retryDelay = _settings?.Where(l => l.LookupCode == "RetryDelay")
            .Select(s => Convert.ToInt32(s.Value)).FirstOrDefault();

        RetryDelay = retryDelay ?? 5;
    }

    private void SetEmailProviderType()
    {
        var providerType = _settings?.Where(l => l.LookupCode == "EmailProviderType")
            .Select(s => s.Value).FirstOrDefault();

        if (providerType != null)
        {
            EmailProviderType = (EmailProviderType)Enum.Parse(typeof(EmailProviderType),
                providerType, ignoreCase: true);
        }
    }
}