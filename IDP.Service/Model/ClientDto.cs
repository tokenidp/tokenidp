namespace IDP.Service.Model;

public class ClientDto
{
    public bool IsValidClient { get; private set; }
    public string Scopes { get; private set; }

    private ClientDto(bool isValidClient, string scopes)
    {
        IsValidClient = isValidClient;
        Scopes = scopes;
    }

    public static ClientDto Create(bool isValidClient, string scopes)
    {
        return new ClientDto(isValidClient, scopes);
    }
}
