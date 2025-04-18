namespace IDP.Service.Application;

public class ClientService
{
    private readonly ClientRepo _clientRepo;
    private readonly IAppLogger<ClientService> _logger;

    public ClientService(ClientRepo clientRepo,
        IAppLogger<ClientService> logger)
    {
        _clientRepo = clientRepo;
        _logger = logger;
    }

    public async Task<TokenType> GetClientTokenType(string clientId)
    {
        var tokenType = await _clientRepo.GetClientTokenType(clientId);

        return tokenType;
    }

    public async Task<ClientDto> GetClientScope(string clientId)
    {
        var client = await _clientRepo.GetClient(clientId);

        if (client == null)
        {
            return ClientDto.Create(false, string.Empty);
        }

        var scopes = string.Join(" ", client.ClientScopes
            .Select(s => s.Scope).ToList());

        return ClientDto.Create(client != null, scopes);
    }

    public async Task<bool> IsValidClient(string clientId)
    {
        var client = await _clientRepo.GetClient(clientId);

       return client != null;
    }
}
