namespace Admin.Core.Clients;

internal sealed class ClientLookups
{
    public List<LookupItem> AppTypes { get; init; } = new();
    public List<LookupItem> ClientTypes { get; init; } = new();
    public List<LookupItem> TokenTypes { get; init; } = new();
    public List<LookupItem> ClientScopes { get; init; } = new();
}