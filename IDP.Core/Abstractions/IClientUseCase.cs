namespace IDP.Core.Abstractions;

public interface IClientUseCase
{
    Task<ClientValidationSnapshot> GetClient(string clientId);

    Task<ClientValidationResult> ValidateClient(string clientId);

    Task<bool> ValidateGrantType(string grantType, string clientId);
}