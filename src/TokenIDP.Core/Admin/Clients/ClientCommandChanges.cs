namespace TokenIDP.Core.Admin.Clients;

internal sealed class ClientCommandChanges
{
    public required List<ClientScope> Scopes { get; init; }
    public required List<ClientGrantType> GrantTypes { get; init; }
    public required List<ClientApiResource> ApiResources { get; init; }
    public ClientSecret? ClientSecret { get; init; }
}
