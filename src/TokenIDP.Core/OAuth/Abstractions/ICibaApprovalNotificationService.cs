namespace TokenIDP.Core.OAuth.Abstractions;

public interface ICibaApprovalNotificationService
{
    Task SendApprovalRequestAsync(
        CibaApprovalNotification notification,
        CancellationToken cancellationToken);
}
