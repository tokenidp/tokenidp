using IDP.Core.Model;

namespace IDP.Foundation.Abstractions.Stores;

public interface IClientStore
{
    Task<ClientValidationSnapshot> GetByClientId(string clientId);

    Task<ClientValidationResult> GetClientValidation(string clientId);
}
