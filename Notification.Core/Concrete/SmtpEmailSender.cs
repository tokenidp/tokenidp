using IDP.Domain.AggregateRoots.Emails;
using IDP.Foundation.Abstractions;
using IDP.Foundation.Emails;
using Notification.Core.Abstractions;
using Notification.Core.Primitives;
using System.Net;
using System.Net.Mail;

namespace Notification.Core.Concrete;

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly IEmailConfigurationProvider _settings;
    private readonly IAppLogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IEmailConfigurationProvider settings,
        IAppLogger<SmtpEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<SendEmailResult> SendAsync(EmailMessage email, CancellationToken ct)
    {
        try
        {
            var to = email.Recipients.Single(r => r.RecipientType == RecipientType.To);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = email.Subject!,
                Body = email.BodyHtml ?? email.BodyText ?? string.Empty,
                IsBodyHtml = email.BodyHtml != null
            };

            mailMessage.To.Add(to.Address);

            foreach (var attachment in email.Attachments)
            {
                if (attachment.StorageMode == 0 && attachment.Content != null)
                {
                    var ms = new MemoryStream(attachment.Content);
                    mailMessage.Attachments.Add(new Attachment(ms, attachment.FileName));
                }
                // BlobRef can be loaded from storage here later
            }

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

