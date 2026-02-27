using IDP.Core.Model;

namespace IDP.Foundation.Abstractions.Stores;

public interface IClientStore
{
    Task<ClientValidationSnapshot> GetActiveByClientId(string clientId);

    Task<ClientShortInfo> GetClientShortInfo(int clientId);

    Task<ClientShortInfo> GetClientShortInfo(string clientId);

    Task<ClientExpiringSecret> GetClientExpiringSecretsAsync(int daysAhead, CancellationToken ct);

    Task<IEnumerable<ClientExternalProviderSnapShort>> GetExternalProviders(int clientId);

    Task<ClientAuthPolicy?> GetClientAuthPolicy(int clientId);
}
