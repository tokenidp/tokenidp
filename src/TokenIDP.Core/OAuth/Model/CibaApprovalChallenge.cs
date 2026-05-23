namespace TokenIDP.Core.OAuth.Model;

public sealed class CibaApprovalChallenge
{
    public required Guid PublicRequestId { get; init; }
    public required int TenantId { get; init; }
    public required int UserId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientName { get; init; }
    public required string BindingMessage { get; init; }
    public required string RequestedScopes { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}
