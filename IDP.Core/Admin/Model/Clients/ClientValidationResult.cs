namespace IDP.Core.Admin.Model.Clients;

internal class ClientValidationResult
{
    public bool IsValidClient { get; private set; }
    public string Scopes { get; private set; }

    private ClientValidationResult(bool isValidClient, string scopes)
    {
        IsValidClient = isValidClient;
        Scopes = scopes;
    }

    public static ClientValidationResult Create(bool isValidClient, string scopes)
    {
        return new ClientValidationResult(isValidClient, scopes);
    }
}
