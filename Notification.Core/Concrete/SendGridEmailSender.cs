using IDP.Domain.AggregateRoots.Emails;
using IDP.Foundation.Abstractions;
using IDP.Foundation.Emails;
using Notification.Core.Abstractions;
using Notification.Core.Primitives;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notification.Core.Concrete;

internal sealed class SendGridEmailSender : IEmailSender
{
    private readonly IEmailConfigurationProvider _settings;
    private readonly IAppLogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IEmailConfigurationProvider settings, IAppLogger<SendGridEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<SendEmailResult> SendAsync(EmailMessage email, CancellationToken ct)
    {
        try
        {
            var to = email.Recipients.Single(r => r.RecipientType == RecipientType.To);

            var client = new SendGridClient(_settings.ApiKey);

            var from = new SendGrid.Helpers.Mail.EmailAddress(_settings.FromEmail, _settings.FromName);
            var toAddr = new SendGrid.Helpers.Mail.EmailAddress(to.Address, to.DisplayName);

            var msg = MailHelper.CreateSingleEmail(
                from,
                toAddr,
                email.Subject!,
                email.BodyText,
                email.BodyHtml);

            // Attachments (inline only; blob refs can be loaded here later)
            foreach (var attachment in email.Attachments)
            {
                if (attachment.StorageMode == 0 && attachment.Content != null)
                {
                    msg.AddAttachment(
                        attachment.FileName,
                        Convert.ToBase64String(attachment.Content),
                        attachment.ContentType);
                }
            }

            var response = await client.SendEmailAsync(msg, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInfo("SendGrid email sent successfully. EmailId={EmailId}", email.Id);
                return SendEmailResult.Ok(response.Headers?.GetValues("X-Message-Id")?.FirstOrDefault());
            }

            // Classify failure type based on HTTP status
            var statusCode = (int)response.StatusCode;
            var body = await response.Body.ReadAsStringAsync(ct);

            _logger.LogError("SendGrid email failed. EmailId={EmailId}, Status={Status}, Body={Body}",
                email.Id, statusCode, body);

            // 4xx (except 429) → permanent failure (bad request, invalid email, etc.)
            if (statusCode >= 400 && statusCode < 500 && statusCode != 429)
            {
                return SendEmailResult.PermanentFailureResult($"SendGrid {statusCode}: {body}");
            }

            // 429 or 5xx → transient
            return SendEmailResult.TransientFailure($"SendGrid {statusCode}: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SendGrid email. EmailId={EmailId}", email.Id);
            return SendEmailResult.TransientFailure(ex.Message);
        }
    }
}