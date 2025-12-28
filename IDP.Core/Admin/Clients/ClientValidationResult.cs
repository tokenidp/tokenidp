namespace IDP.Core.Admin.Clients;

internal class ClientValidationResult
{
    public bool IsValidClient { get; private set; }

    private ClientValidationResult(bool isValidClient)
    {
        IsValidClient = isValidClient;
    }

    public static ClientValidationResult Create(bool isValidClient)
    {
        return new ClientValidationResult(isValidClient);
    }
}
