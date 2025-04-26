using System.Net;
using System.Net.Mail;

namespace Services.Common.Notifications;

public class SmtpEmail : IEmailNotification
{
    private readonly IEmailSetting _notificationSettings;
    private readonly IAppLogger<SmtpEmail> _logger;

    public SmtpEmail(IEmailSetting notificationSettings,
        IAppLogger<SmtpEmail> logger)
    {
        _notificationSettings = notificationSettings;
        _logger = logger;
    }

    public async Task<bool> SendNotificationAsync(NotificationRequest request)
    {
        try
        {
            _logger.LogInfo("Preparing to send email to {Recipient}", request.Recipient);

            string htmlContent = null;
            if (!string.IsNullOrEmpty(request.HtmlContent) && request.Tokens != null)
            {
                _logger.LogDebug("Replacing tokens in HTML content");
                htmlContent = ReplaceTokens(request.HtmlContent, request.Tokens);
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_notificationSettings.FromEmail, _notificationSettings.FromName),
                Subject = request.Subject,
                Body = htmlContent ?? request.Message,
                IsBodyHtml = htmlContent != null
            };
            mailMessage.To.Add(request.Recipient);

            _logger.LogDebug("Email message constructed. From: {From}, To: {To}, Subject: {Subject}, Body length: {BodyLength}",
                mailMessage.From, mailMessage.To, mailMessage.Subject, mailMessage.Body.Length);

            if (request.Attachments != null && request.Attachments.Any())
            {
                _logger.LogInfo("Adding {AttachmentCount} attachments", request.Attachments.Count);
                AddAttachments(mailMessage, request.Attachments);
            }

            using (var smtpClient = new SmtpClient(_notificationSettings.SmtpServer, _notificationSettings.SmtpPort))
            {
                _logger.LogDebug("Configuring SMTP client. Server: {SmtpServer}:{SmtpPort}, SSL: {UseSsl}",
                    _notificationSettings.SmtpServer, _notificationSettings.SmtpPort, _notificationSettings.SmtpUseSsl);

                if (!string.IsNullOrEmpty(_notificationSettings.SmtpUsername))
                {
                    smtpClient.UseDefaultCredentials = false; // Explicitly set to false when providing credentials
                    smtpClient.Credentials = new NetworkCredential(
                        _notificationSettings.SmtpUsername,
                        _notificationSettings.SmtpPassword
                    );
                    _logger.LogDebug("SMTP credentials set (Username: {SmtpUser})", _notificationSettings.SmtpUsername);
                }

                smtpClient.EnableSsl = _notificationSettings.SmtpUseSsl;

                _logger.LogInfo("Attempting to send email...");
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInfo("Email sent successfully to {Recipient}", request.Recipient);

                return true;
            }
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error sending email to {Recipient}. Status: {StatusCode}",
                request.Recipient, smtpEx.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", request.Recipient);
            return false;
        }
    }

    public bool ValidateRecipient(string recipient)
    {
        // Validate if the recipient is a valid email address
        var emailRegex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        return emailRegex.IsMatch(recipient);
    }

    private static void AddAttachments(MailMessage mailMessage, Dictionary<string, string> attachments)
    {
        if (attachments == null || attachments.Count == 0)
        {
            return;
        }

        foreach (var attachment in attachments)
        {
            byte[] bytesArray = System.IO.File.ReadAllBytes(attachment.Value);

            MemoryStream memoryStream = new MemoryStream(bytesArray);
            System.Net.Mail.Attachment attachmentItem = new(memoryStream, attachment.Key);
            mailMessage.Attachments.Add(attachmentItem);
        }
    }

    private static string ReplaceTokens(string template, Dictionary<string, string> tokens)
    {
        //For example - { "<%CLIENTNAME%>", "Acme Corp" }

        foreach (var token in tokens)
        {
            template = template.Replace(token.Key, token.Value, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }
}

