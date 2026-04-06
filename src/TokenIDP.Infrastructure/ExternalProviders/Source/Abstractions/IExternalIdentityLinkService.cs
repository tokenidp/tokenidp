using TokenIDP.Domain.AggregateRoots.Users;
using TokenIDP.Core.OAuth.ExternalProviders.Model;

namespace TokenIDP.Core.OAuth.ExternalProviders.Abstractions;

public interface IExternalIdentityLinkService
{
    Task<User> FindOrProvisionUserAsync(
        int tenantId,
        int clientId,
        ExternalIdentity identity,
        CancellationToken cancellationToken);
}
