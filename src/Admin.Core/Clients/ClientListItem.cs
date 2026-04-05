namespace Admin.Core.Clients;

internal class ClientListItem
{
    internal static Expression<Func<Client, ClientListItem>> Projection =>
        client => new ClientListItem()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            AppType = client.ClientType,
            TokenType = client.TokenType,
            IsActive = client.IsActive
        };

    public int Id { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public ClientTypes AppType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public bool IsActive { get; private set; }
}