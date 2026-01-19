namespace Admin.Core.Clients;

internal class ClientListItem
{
    internal static Expression<Func<Client, ClientListItem>> Projection =>
        client => new ClientListItem()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            ClientType = client.ClientType,
            AppType = client.AppType,
            TokenType = client.TokenType,
            IsActive = client.IsActive
        };

    public int Id { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public ClientTypes ClientType { get; private set; }
    public AppTypes AppType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public bool IsActive { get; private set; }
}