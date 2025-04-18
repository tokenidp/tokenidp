using Services.Common.Notifications;

namespace IDP.Service.Application.TokenService;

public class EmailProviderFactory
{
    private readonly Func<EmailProviderType, IEmailNotification> _notification;

    public EmailProviderFactory(Func<EmailProviderType, IEmailNotification> notification)
    {
        _notification = notification;
    }

    public IEmailNotification GetService(EmailProviderType tokenType)
    {
        return _notification(tokenType);
    }
}
