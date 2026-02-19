using IDP.Domain.AggregateRoots.Emails;
using IDP.Infrastructure.Emails.Primitives;

namespace IDP.Infrastructure.Emails.Abstractions;

public interface IEmailSender
{
    /// <summary>
    /// Sends a notification to the specified recipient.
    /// </summary>
    /// <param name="recipient">Recipient's contact information (email, phone number, etc.).</param>
    /// <param name="message">The notification message content.</param>
    /// <param name="subject">Optional subject for the notification (e.g., for email notifications).</param>
    /// <returns>A task representing the asynchronous operation of sending the notification.</returns>
    Task<SendEmailResult> SendAsync(EmailMessage email, CancellationToken ct);
}
