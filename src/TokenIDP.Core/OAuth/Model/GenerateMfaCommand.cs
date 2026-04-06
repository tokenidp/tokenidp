namespace TokenIDP.Core.OAuth.Model;

public sealed class GenerateMfaCommand
{
    public int UserId { get; init; }
    public int TenantId { get; init; }
    public string? ClientId { get; init; }
    public string? Scopes { get; init; }
}
