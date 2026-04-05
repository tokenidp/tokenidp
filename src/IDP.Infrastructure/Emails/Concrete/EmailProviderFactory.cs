using IDP.Infrastructure.Emails.Abstractions;
using IDP.Infrastructure.Emails.Primitives;

namespace IDP.Infrastructure.Emails.Concrete;

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
