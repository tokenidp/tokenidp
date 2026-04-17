namespace TokenIDP.Core.OAuth.Model;

public sealed class CibaPendingRequest
{
    public int Id { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string RequestedScopes { get; init; } = string.Empty;
    public string? BindingMessage { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
