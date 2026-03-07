using IDP.Domain.AggregateRoots.Users;
using IDP.ExternalProviders.Model;

namespace IDP.ExternalProviders.Abstractions;

public interface IExternalIdentityLinkService
{
    Task<User> FindOrProvisionUserAsync(
        int tenantId,
        int clientId,
        ExternalIdentity identity,
        CancellationToken cancellationToken);
}