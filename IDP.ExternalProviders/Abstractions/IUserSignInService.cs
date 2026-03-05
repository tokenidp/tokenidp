using IDP.Domain.AggregateRoots.Users;

namespace IDP.ExternalProviders.Abstractions;

public interface IUserSignInService
{
    Task SignInAsync(
        User user,
        int tenantId,
        CancellationToken cancellationToken);
}
