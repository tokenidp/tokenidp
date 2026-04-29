using System.Net;
using System.Net.Mail;
using TokenIDP.Core.Abstractions;
using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Infrastructure.Emails.Abstractions;
using TokenIDP.Infrastructure.Emails.Primitives;

namespace TokenIDP.Infrastructure.Emails.Concrete;

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly IAppLogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IAppLogger<SmtpEmailSender> logger)
    {
        _logger = logger;
    }

    public async Task<SendEmailResult> SendAsync(EmailConfigurationProvider settings,
        EmailMessage email,
        CancellationToken ct)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = email.Subject!,
                Body = email.BodyHtml ?? email.BodyText ?? string.Empty,
                IsBodyHtml = email.BodyHtml != null
            };

            mailMessage.To.Add(email.ToAddress);

            using var smtpClient = new SmtpClient(settings.SmtpServer, settings.SmtpPort)
            {
                EnableSsl = settings.SmtpUseSsl
            };

            if (!string.IsNullOrEmpty(settings.SmtpUsername))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(
                    settings.SmtpUsername,
                    settings.SmtpPassword);
            }

            await smtpClient.SendMailAsync(mailMessage, ct);

            _logger.LogInfo("Email sent successfully. EmailId={EmailId}", email.Id);
            return SendEmailResult.Ok();
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error sending email. EmailId={EmailId}", email.Id);

            // classify permanent vs transient
            return smtpEx.StatusCode == SmtpStatusCode.MailboxUnavailable
                ? SendEmailResult.PermanentFailureResult(smtpEx.Message)
                : SendEmailResult.TransientFailure(smtpEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email. EmailId={EmailId}", email.Id);
            return SendEmailResult.TransientFailure(ex.Message);
        }
    }
}


