using IDP.Domain.AggregateRoots.Users;

namespace IDP.Foundation.Abstractions.Stores;

public interface IIdentityStore
{
    Task<AuthenticationContext> Authenticate(string userName, string password);

    Task<User?> FindByIdAsync(string id);
}
