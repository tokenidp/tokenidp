using IDP.Domain.AggregateRoots.Clients;

namespace IDP.Foundation.Abstractions;

public interface IClientStore
{
    Task<ClientValidationSnapshot> GetByClientId(string clientId);
}
