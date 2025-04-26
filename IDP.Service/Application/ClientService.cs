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

    public async Task<ClientDto> GetClientScopes(string clientId)
    {
        var scopes = await _clientRepo.GetClientScopes(clientId);

        if (string.IsNullOrEmpty(scopes))
        {
            return ClientDto.Create(false, string.Empty);
        }

        return ClientDto.Create(true, scopes);
    }

    public async Task<bool> IsValidClient(string clientId)
    {
        var scopes = await _clientRepo.GetClientScopes(clientId);

        return !string.IsNullOrEmpty(scopes);
    }
}
