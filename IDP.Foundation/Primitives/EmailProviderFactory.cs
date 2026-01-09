using IDP.Foundation.Abstractions;
using IDP.Foundation.Contracts;

namespace IDP.Foundation.Primitives;

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
