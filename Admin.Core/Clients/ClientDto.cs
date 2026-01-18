namespace Admin.Core.Clients;

internal class ClientDto
{
    internal static Expression<Func<Client, ClientDto>> Projection =>
    client => new ClientDto()
    {
        Id = client.Id,
        ClientId = client.ClientId,
        ClientName = client.ClientName,
        Description = client.Description,
        ClientType = client.ClientType,
        AppType = client.AppType,
        AccessTokenType = client.TokenType,
        IsActive = client.IsActive
    };


    public int Id { get; set; }
    public string ClientId { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ClientTypes ClientType { get; private set; }
    public AppTypes AppType { get; private set; }
    public TokenTypes AccessTokenType { get; private set; }
    public bool IsActive { get; private set; }
}



