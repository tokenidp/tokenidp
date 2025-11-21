namespace IDP.Core.Notifications;

public interface IEmailSetting
{
    public string ApiKey { get; }
    public string SmtpServer { get; }
    public int SmtpPort { get; }
    public string SmtpUsername { get; }
    public string SmtpPassword { get; }
    public bool SmtpUseSsl { get; }
    public string FromEmail { get; }
    public string FromName { get; }
    public int RetryAttempts { get; }
    public int RetryDelay { get; }
    public EmailProviderType EmailProviderType { get; }

    Task PopulateEmailSettings(int tenantId);
}
