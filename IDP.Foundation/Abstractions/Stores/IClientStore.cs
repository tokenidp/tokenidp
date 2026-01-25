using IDP.Domain.AggregateRoots.Clients;

namespace IDP.Foundation.Abstractions.Stores;

public interface IClientStore
{
    Task<ClientValidationSnapshot> GetByClientId(string clientId);
}
