namespace Admin.Core.Clients;

internal sealed class ClientLookups
{
    public List<LookupItem> AppTypes { get; init; } = new();
    public List<LookupItem> GrantTypes { get; init; } = new();
    public List<LookupItem> TokenTypes { get; init; } = new();
    public List<LookupItem> ClientScopes { get; init; } = new();
    public List<LookupItem> ExternalProviders { get; init; } = new();
    public List<LookupItem> Roles { get; init; } = new();
}