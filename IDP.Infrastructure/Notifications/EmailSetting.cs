using IDP.Domain.AggregateRoots.Configurations;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Infrastructure.Notifications;

internal sealed class EmailSetting : IEmailSetting
{
    private readonly IConfigurationStore _configService;
    private IEnumerable<ConfigurationShortInfo>? _settings;

    public EmailSetting(IConfigurationStore configService)
    {
        _configService = configService;

        ApiKey = string.Empty;
        SmtpServer = string.Empty;
        SmtpPort = 0;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;
        SmtpUseSsl = true;
        FromEmail = string.Empty;
        FromName = string.Empty;
        RetryAttempts = 3;
        RetryDelay = 5;
    }

    public string ApiKey { get; private set; }
    public string SmtpServer { get; private set; }
    public int SmtpPort { get; private set; }
    public string SmtpUsername { get; private set; }
    public string SmtpPassword { get; private set; }
    public bool SmtpUseSsl { get; private set; }
    public string FromEmail { get; private set; }
    public string FromName { get; private set; }
    public int RetryAttempts { get; private set; }
    public int RetryDelay { get; private set; }
    public EmailProviderType EmailProviderType { get; private set; }

    public async Task PopulateEmailSettings(int tenantId)
    {
        _settings = await _configService
            .GetTenantConfigurations(tenantId, ConfigurationScopes.Notification);

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
        var apikey = _settings?.Where(l => l.ConfigKey == "ApiKey")
            .Select(s => s.ConfigValue).FirstOrDefault();

        ApiKey = apikey ?? ApiKey;
    }

    private void SetSmtpServer()
    {
        var smtpServer = _settings?.Where(l => l.ConfigKey == "SmtpServer")
            .Select(s => s.ConfigValue).FirstOrDefault();

        SmtpServer = smtpServer ?? SmtpServer;
    }

    private void SetSmtpPort()
    {
        var smtpPort = _settings?.Where(l => l.ConfigKey == "SmtpPort")
            .Select(s => Convert.ToInt32(s.ConfigValue)).FirstOrDefault();

        SmtpPort = smtpPort ?? SmtpPort;
    }

    private void SetSmtpUsername()
    {
        var smtpUsername = _settings?.Where(l => l.ConfigKey == "SmtpUsername")
            .Select(s => s.ConfigValue).FirstOrDefault();

        SmtpUsername = smtpUsername ?? SmtpUsername;
    }

    private void SetSmtpPassword()
    {
        var smtpPassword = _settings?.Where(l => l.ConfigKey == "SmtpPassword")
            .Select(s => s.ConfigValue).FirstOrDefault();

        SmtpPassword = smtpPassword ?? SmtpPassword;
    }

    private void SetSmtpUseSsl()
    {
        var smtpUseSsl = _settings?.Where(l => l.ConfigKey == "SmtpUseSsl")
            .Select(s => Convert.ToBoolean(s.ConfigValue)).FirstOrDefault();

        SmtpUseSsl = smtpUseSsl ?? SmtpUseSsl;
    }

    private void SetFromEmail()
    {
        var fromEmail = _settings?.Where(l => l.ConfigKey == "FromEmail")
            .Select(s => s.ConfigValue).FirstOrDefault();

        FromEmail = fromEmail ?? FromEmail;
    }

    private void SetFromName()
    {
        var fromName = _settings?.Where(l => l.ConfigKey == "FromName")
            .Select(s => s.ConfigValue).FirstOrDefault();

        FromName = fromName ?? FromName;
    }

    private void SetRetryAttempts()
    {
        var retryAttempts = _settings?.Where(l => l.ConfigKey == "RetryAttempts")
            .Select(s => Convert.ToInt32(s.ConfigValue)).FirstOrDefault();

        RetryAttempts = retryAttempts ?? RetryAttempts;
    }

    private void SetRetryDelay()
    {
        var retryDelay = _settings?.Where(l => l.ConfigKey == "RetryDelay")
            .Select(s => Convert.ToInt32(s.ConfigValue)).FirstOrDefault();

        RetryDelay = retryDelay ?? RetryDelay;
    }

    private void SetEmailProviderType()
    {
        var providerType = _settings?.Where(l => l.ConfigKey == "EmailProviderType")
            .Select(s => s.ConfigValue).FirstOrDefault();

        if (providerType != null)
        {
            EmailProviderType = (EmailProviderType)Enum.Parse(typeof(EmailProviderType),
                providerType, ignoreCase: true);
        }
    }
}