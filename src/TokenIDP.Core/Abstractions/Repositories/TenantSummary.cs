namespace TokenIDP.Core.Abstractions.Repositories;

public sealed class TenantSummary
{
    public int Id { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string TenantDisplayName { get; init; } = string.Empty;
    public string TenantKey { get; init; } = string.Empty;
}
