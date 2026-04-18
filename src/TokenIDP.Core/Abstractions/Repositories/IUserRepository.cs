using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Core.Admin.Users;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User> GetUserById(int id);
    Task<UserShortInfo> GetUserShortInfo(int id);
    Task<int> CreateUser(User user, string password);
    Task<int> UpdateUser(User user);
    Task<int> DeleteAsync(User user, CancellationToken ct);
    Task<User?> GetUserAggregateAsync(int id, int tenantId, CancellationToken ct);
    Task<UserDetail?> GetUserDetailAsync(int tenantId, int userId, CancellationToken ct);
    Task<PaginatedList<UserSearchResult>> SearchUsersAsync(int tenantId, SearchData request, CancellationToken ct);
    Task<PaginatedList<UserSearchResult>> SearchUsersByRoleAsync(
        int tenantId,
        int roleId,
        SearchData request,
        CancellationToken ct);
    Task<UserLookups> GetUserLookupsAsync(int tenantId, CancellationToken ct);
    Task<IReadOnlyList<PermissionInfo>> GetUserPermissionsAsync(int userId, CancellationToken ct);
    Task<User?> GetByTenantAndEmailAsync(int tenantId, string email, CancellationToken ct);
    Task<User?> FindByLoginHintAsync(int tenantId, string loginHint, CancellationToken ct);
    Task<User?> GetByTenantAsync(int userId, int tenantId, CancellationToken ct);
    Task<bool> EmailExistsAsync(int tenantId, int excludeUserId, string normalizedEmail, CancellationToken ct);
    Task<bool> UserNameExistsAsync(int tenantId, int excludeUserId, string normalizedUserName, CancellationToken ct);
    Task CreatePasswordResetAsync(User user, PasswordResetToken resetToken, CancellationToken ct);
    Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(byte[] tokenHash, DateTime nowUtc, CancellationToken ct);
    Task CreateEmailConfirmationAsync(EmailConfirmationToken confirmationToken, CancellationToken ct);
    Task<EmailConfirmationToken?> GetValidEmailConfirmationTokenAsync(byte[] tokenHash, DateTime nowUtc, CancellationToken ct);
}

