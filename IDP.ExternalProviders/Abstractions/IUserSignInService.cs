using IDP.Domain.AggregateRoots.Users;

namespace IDP.ExternalProviders.Abstractions;

public interface IUserSignInService
{
    Task SignInAsync(
        int userId,
        string userName,
        string email,
        int tenantId,
        CancellationToken cancellationToken);
}
