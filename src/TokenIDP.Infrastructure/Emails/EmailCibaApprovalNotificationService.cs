using System.Net;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.Abstractions;
using TokenIDP.Core.OAuth.Model;
using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Domain.AggregateRoots.Emails.ValueObjects;

namespace TokenIDP.Infrastructure.Emails;

internal sealed class EmailCibaApprovalNotificationService : ICibaApprovalNotificationService
{
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly ICurrentUserService _currentUserService;

    public EmailCibaApprovalNotificationService(
        IEmailQueueRepository emailQueueRepository,
        ICurrentUserService currentUserService)
    {
        _emailQueueRepository = emailQueueRepository;
        _currentUserService = currentUserService;
    }

    public Task SendApprovalRequestAsync(
        CibaApprovalNotification notification,
        CancellationToken cancellationToken)
    {
        var subject = $"Approve sign-in request for {notification.ClientName}";
        var scopeList = string.Join(", ", notification.RequestedScopes);
        var expiresAt = notification.ExpiresAtUtc.UtcDateTime.ToString("u");

        var bodyText =
            $"Client: {notification.ClientName}\n" +
            $"Binding message: {notification.BindingMessage}\n" +
            $"Requested scopes: {scopeList}\n" +
            $"Expires: {expiresAt}\n\n" +
            $"Open this link to review the request: {notification.ApprovalUrl}\n\n" +
            "Only approve this request if you initiated it.";

        var bodyHtml =
            "<p>A sign-in approval request is waiting for you.</p>" +
            $"<p><strong>Client:</strong> {Encode(notification.ClientName)}</p>" +
            $"<p><strong>Binding message:</strong> {Encode(notification.BindingMessage)}</p>" +
            $"<p><strong>Requested scopes:</strong> {Encode(scopeList)}</p>" +
            $"<p><strong>Expires:</strong> {Encode(expiresAt)}</p>" +
            $"<p><a href=\"{Encode(notification.ApprovalUrl)}\">Review sign-in request</a></p>" +
            "<p><strong>Security warning:</strong> only approve this request if you initiated it.</p>";

        var email = EmailMessage.CreateRendered(
            tenantId: notification.TenantId,
            messageKey: $"ciba-approval:{notification.TenantId}:{notification.UserId}:{notification.ExpiresAtUtc.ToUnixTimeSeconds()}",
            recipient: new EmailRecipient(new EmailAddress(notification.UserEmail), notification.UserEmail),
            subject: subject,
            bodyHtml: bodyHtml,
            bodyText: bodyText,
            priority: 2,
            maxAttempts: 10,
            scheduledAtUtc: DateTime.UtcNow,
            correlationId: _currentUserService.CorrelationId,
            tags: "ciba-approval");

        return _emailQueueRepository.EnqueueAsync(email, cancellationToken);
    }

    private static string Encode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}
