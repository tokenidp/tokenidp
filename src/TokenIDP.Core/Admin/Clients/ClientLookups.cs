namespace TokenIDP.Core.Admin.Clients;

internal sealed class ClientLookups
{
    public List<LookupItem> AppTypes { get; init; } = new();
    public List<LookupItem> GrantTypes { get; init; } = new();
    public List<LookupItem> TokenTypes { get; init; } = new();
    public List<LookupItem> ClientScopes { get; init; } = new();
    public List<ApiResourceLookup> ApiResources { get; init; } = new();
    public List<LookupItem> ExternalProviders { get; init; } = new();
    public List<LookupItem> Roles { get; init; } = new();
}

public sealed class ApiResourceLookup
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<ApiScopeLookup> Scopes { get; init; } = new();
}

public sealed class ApiScopeLookup
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
