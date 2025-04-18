using System.Net;
using System.Net.Mail;

namespace Services.Common.Notifications;

public class SmtpEmail : IEmailNotification
{
    private readonly IEmailSetting _notificationSettings;

    public SmtpEmail(IEmailSetting notificationSettings)
    {
        _notificationSettings = notificationSettings;
    }

    public async Task<bool> SendNotificationAsync(NotificationRequest request)
    {
        string htmlContent = null;
        if (!string.IsNullOrEmpty(request.HtmlContent) && request.Tokens != null)
        {
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

        AddAttachments(mailMessage, request.Attachments);

        using (var smtpClient = new SmtpClient(_notificationSettings.SmtpServer, _notificationSettings.SmtpPort))
        {
            if (!string.IsNullOrEmpty(_notificationSettings.SmtpUsername))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(_notificationSettings.SmtpUsername,
                    _notificationSettings.SmtpPassword);
            }

            smtpClient.EnableSsl = _notificationSettings.SmtpUseSsl;
            await smtpClient.SendMailAsync(mailMessage);
            return true;
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

