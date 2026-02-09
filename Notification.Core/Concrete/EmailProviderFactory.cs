using IDP.Foundation.Emails;
using Notification.Core.Primitives;

namespace Notification.Core.Concrete;

public class EmailProviderFactory
{
    private readonly Func<EmailProviderType, IEmailSender> _notification;

    public EmailProviderFactory(Func<EmailProviderType, IEmailSender> notification)
    {
        _notification = notification;
    }

    public IEmailSender GetService(EmailProviderType tokenType)
    {
        return _notification(tokenType);
    }
}
