using TokenIDP.Infrastructure.Emails.Abstractions;
using TokenIDP.Infrastructure.Emails.Primitives;

namespace TokenIDP.Infrastructure.Emails.Concrete;

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

