using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Infrastructure.Emails.Abstractions;
using TokenIDP.Infrastructure.Emails.Primitives;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace TokenIDP.Infrastructure.Emails.Concrete;

internal sealed class SendGridEmailSender : IEmailSender
{
    private readonly IAppLogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IAppLogger<SendGridEmailSender> logger)
    {
        _logger = logger;
    }

    public async Task<SendEmailResult> SendAsync(EmailConfigurationProvider settings,
        EmailMessage email,
        CancellationToken ct)
    {
        try
        {
            var client = new SendGridClient(settings.ApiKey);

            var from = new SendGrid.Helpers.Mail.EmailAddress(settings.FromEmail, settings.FromName);
            var toAddr = new SendGrid.Helpers.Mail.EmailAddress(email.ToAddress, email.DisplayName);

            var msg = MailHelper.CreateSingleEmail(
                from,
                toAddr,
                email.Subject!,
                email.BodyText,
                email.BodyHtml);

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

            // 4xx (except 429) ? permanent failure (bad request, invalid email, etc.)
            if (statusCode >= 400 && statusCode < 500 && statusCode != 429)
            {
                return SendEmailResult.PermanentFailureResult($"SendGrid {statusCode}: {body}");
            }

            // 429 or 5xx ? transient
            return SendEmailResult.TransientFailure($"SendGrid {statusCode}: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SendGrid email. EmailId={EmailId}", email.Id);
            return SendEmailResult.TransientFailure(ex.Message);
        }
    }
}
