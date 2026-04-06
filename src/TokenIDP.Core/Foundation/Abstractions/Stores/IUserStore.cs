namespace TokenIDP.Core.Foundation.Abstractions.Stores;

public interface IUserStore
{
    Task<User> GetUserById(int id);
    Task<UserShortInfo> GetUserShortInfo(int id);
    Task<int> CreateUser(User user, string password);
    Task<int> UpdateUser(User user);
    Task<User?> GetUserAggregateAsync(int id, CancellationToken ct);
    Task<bool> EmailExistsAsync(int excludeUserId, string normalizedEmail, CancellationToken ct);
    Task<bool> UserNameExistsAsync(int excludeUserId, string normalizedUserName, CancellationToken ct);
}

