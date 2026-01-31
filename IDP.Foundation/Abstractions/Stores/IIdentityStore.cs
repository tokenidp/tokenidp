namespace IDP.Foundation.Abstractions.Stores;

public interface IIdentityStore
{
    Task<AuthenticationContext> Authenticate(string userName, string password);

    Task<User> GetUserById(int id);

    Task<UserShortInfo> GetUserShortInfo(int id);

    Task<int> SaveChangesAsync();
}
