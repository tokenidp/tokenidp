using IDP.Domain.AggregateRoots.Users;

namespace IDP.Foundation.Abstractions;

public interface IIdentityStore
{
    Task<AuthenticationResult> Authenticate(string userName, string password);

    Task<User> FindByIdAsync(string id);
}
