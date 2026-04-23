using TokenIDP.Core.OAuth.Model;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Clients;
using TokenIDP.Core.Admin.Common;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IClientRepository
{
    Task<ClientValidationSnapshot> GetActiveByClientId(string clientId);

    Task<ClientShortInfo> GetClientShortInfo(int clientId);

    Task<ClientShortInfo> GetClientShortInfo(string clientId);
    Task<ClientRateLimitProfile?> FindRateLimitProfileAsync(string clientId, CancellationToken ct);

    Task<ClientExpiringSecret> GetClientExpiringSecretsAsync(int daysAhead, CancellationToken ct);
    Task<ClientDetail?> GetClientDetailAsync(int tenantId, int clientId, CancellationToken ct);
    Task<PaginatedList<ClientDetail>> SearchClientsAsync(int tenantId, SearchData request, CancellationToken ct);
    Task<ClientLookups> GetClientLookupsAsync(int tenantId, CancellationToken ct);
    Task<bool> ClientIdExistsAsync(int tenantId, string clientId, CancellationToken ct);
    Task<bool> ClientIdExistsGloballyAsync(string clientId, CancellationToken ct);
    Task<IReadOnlyList<int>> GetTenantClientIdsAsync(int tenantId, CancellationToken ct);

    Task<IEnumerable<ClientExternalProviderSnapshot>> GetExternalProviders(int clientId);

    Task<ClientAuthPolicy?> GetClientAuthPolicy(int clientId);
    Task<Client?> GetClientAggregateAsync(int clientId, int tenantId, CancellationToken ct);
    Task<int> AddAsync(Client client, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<int> DeleteAsync(Client client, CancellationToken ct);
    Task<IReadOnlyList<LookupItem>> GetTokenClientLookupsAsync(int tenantId, int limit, CancellationToken ct);
}

