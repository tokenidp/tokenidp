namespace Admin.Core.Tokens;

internal sealed class TokenRevokeRequest
{
    public string? Reason { get; init; }
}