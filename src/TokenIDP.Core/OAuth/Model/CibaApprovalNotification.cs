namespace TokenIDP.Core.OAuth.Model;

public sealed class CibaApprovalNotification
{
    public required int TenantId { get; init; }
    public required int UserId { get; init; }
    public required string UserEmail { get; init; }
    public required string ClientName { get; init; }
    public required string BindingMessage { get; init; }
    public required string ApprovalUrl { get; init; }
    public required DateTimeOffset ExpiresAtUtc { get; init; }
    public required IReadOnlyCollection<string> RequestedScopes { get; init; }
}
