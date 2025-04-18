namespace Services.Common.Notifications;

public interface IEmailNotification
{
    /// <summary>
    /// Sends a notification to the specified recipient.
    /// </summary>
    /// <param name="recipient">Recipient's contact information (email, phone number, etc.).</param>
    /// <param name="message">The notification message content.</param>
    /// <param name="subject">Optional subject for the notification (e.g., for email notifications).</param>
    /// <returns>A task representing the asynchronous operation of sending the notification.</returns>
    Task<bool> SendNotificationAsync(NotificationRequest request);

    /// <summary>
    /// Validates the recipient's contact information before sending the notification.
    /// </summary>
    /// <param name="recipient">Recipient's contact information.</param>
    /// <returns>True if the recipient's contact information is valid, false otherwise.</returns>
    bool ValidateRecipient(string recipient);
}
