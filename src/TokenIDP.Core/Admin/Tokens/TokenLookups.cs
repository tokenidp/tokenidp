namespace TokenIDP.Core.Admin.Tokens;

internal sealed class TokenLookups
{
    public List<LookupItem> TokenTypes { get; init; } = new();
    public List<LookupItem> Statuses { get; init; } = new();
    public List<LookupItem> Clients { get; init; } = new();
}
