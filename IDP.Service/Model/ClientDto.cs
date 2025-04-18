namespace IDP.Service.Model;

public class ClientDto
{
    public bool IsValidClient { get; }
    public string Scopes { get; }

    private ClientDto(bool isValidClient, string scopes)
    {
        IsValidClient = isValidClient;
        Scopes = scopes;
    }

    public static ClientDto Create(bool isValidClient, string scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes))
            throw new ArgumentException("Scopes cannot be null or empty", nameof(scopes));

        return new ClientDto(isValidClient, scopes);
    }
}
