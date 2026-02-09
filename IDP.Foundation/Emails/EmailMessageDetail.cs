namespace IDP.Foundation.Emails;

public sealed class EmailMessageDetail
{
    public string Recipient { get; private set; }
    public string RecipientName { get; private set; }
    public Dictionary<string, string>? Tokens { get; private set; }
    public string? Subject { get; private set; }
    public string? Message { get; private set; }
    public string? HtmlContent { get; private set; }
    public Dictionary<string, string>? Attachments { get; private set; }

    public EmailMessageDetail(string recipient,
        string recipientName,
        Dictionary<string, string>? tokens,
        string? subject,
        string? message,
        string? htmlContent,
        Dictionary<string, string>? attachments)
    {
        Recipient = recipient;
        RecipientName = recipientName;
        Tokens = tokens;
        Subject = subject;
        Message = message;
        HtmlContent = htmlContent;
        Attachments = attachments;
    }

    public static EmailMessageDetail Create(string recipient,
        string recipientName,
        Dictionary<string, string>? tokens = null,
        string? subject = null,
        string? message = null,
        string? htmlContent = null,
        Dictionary<string, string>? attachments = null)
    {
        return new EmailMessageDetail(recipient,
            recipientName,
            tokens,
            subject,
            message,
            htmlContent,
            attachments);
    }
}
