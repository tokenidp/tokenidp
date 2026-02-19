using IDP.Domain.AggregateRoots.Emails;
using IDP.Infrastructure.Emails.Abstractions;
using IDP.Infrastructure.Emails.Primitives;
using System.Net;
using System.Net.Mail;

namespace IDP.Infrastructure.Emails.Concrete;

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailConfigurationProvider _settings;
    private readonly IAppLogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(EmailConfigurationProvider settings,
        IAppLogger<SmtpEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<SendEmailResult> SendAsync(EmailMessage email, CancellationToken ct)
    {
        try
        {           
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = email.Subject!,
                Body = email.BodyHtml ?? email.BodyText ?? string.Empty,
                IsBodyHtml = email.BodyHtml != null
            };

            mailMessage.To.Add(email.ToAddress);
        
            using var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpUseSsl
            };

            if (!string.IsNullOrEmpty(_settings.SmtpUsername))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(
                    _settings.SmtpUsername,
                    _settings.SmtpPassword);
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

