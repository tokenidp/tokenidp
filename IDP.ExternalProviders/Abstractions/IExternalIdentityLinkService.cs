using IDP.Domain.AggregateRoots.Users;
using IDP.ExternalProviders.Model;

namespace IDP.ExternalProviders.Abstractions;

public interface IExternalIdentityLinkService
{
    Task<User> FindOrProvisionUserAsync(int tenantId, ExternalIdentity identity,
     CancellationToken cancellationToken);
}
