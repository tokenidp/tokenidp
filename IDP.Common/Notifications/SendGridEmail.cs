using IDP.Common.Notifications;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace IDP.Core.Notifications;

public class SendGridEmail : IEmailNotification
{
    private readonly IEmailSetting _notificationSettings;

    public SendGridEmail(IEmailSetting notificationSettings)
    {
        _notificationSettings = notificationSettings;
    }

    public async Task<bool> SendNotificationAsync(NotificationRequest request)
    {
        string? htmlContent = null;
        if (!string.IsNullOrEmpty(request.HtmlContent) && request.Tokens != null)
        {
            htmlContent = ReplaceTokens(htmlContent, request.Tokens);
        }

        var client = new SendGridClient(_notificationSettings.ApiKey);
        var from = new EmailAddress(_notificationSettings.FromEmail, _notificationSettings.FromName);
        var to = new EmailAddress(request.Recipient, request.RecipientName);
        var msg = MailHelper.CreateSingleEmail(from, to, request.Subject, request.Message, htmlContent);
        var response = await client.SendEmailAsync(msg);

        return response.IsSuccessStatusCode;
    }

    public bool ValidateRecipient(string recipient)
    {
        // Validate if the recipient is a valid email address
        var emailRegex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        return emailRegex.IsMatch(recipient);
    }

    private static string ReplaceTokens(string? template, Dictionary<string, string> tokens)
    {
        if (template == null)
        {
            return string.Empty;
        }

        //For example - { "<%CLIENTNAME%>", "Acme Corp" }

        foreach (var token in tokens)
        {
            template = template.Replace(token.Key, token.Value, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }
}