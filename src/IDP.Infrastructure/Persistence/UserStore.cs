using Admin.Core.Common;
using IDP.Foundation.Abstractions.Stores;
using IDP.Infrastructure.Projections;

namespace IDP.Infrastructure.Persistence;

public class UserStore : IUserStore
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly IAppLogger<UserStore> _logger;
    private readonly PasswordService _passwordService;

    public UserStore(IAppLogger<UserStore> logger,
        IApplicationDbContext applicationDbContext,
        IApplicationEventDispatcher applicationEventDispatcher,
        PasswordService passwordService)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _passwordService = passwordService;
    }

    public async Task<User> GetUserById(int id)
    {
        var user = await _applicationDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return user!;
    }

    public async Task<UserShortInfo> GetUserShortInfo(int id)
    {
        var user = await _applicationDbContext.Users
            .Where(u => u.Id == id)
            .AsNoTracking()
            .Select(UserProjection.Projection)
            .FirstOrDefaultAsync();

        return user!;
    }

    public async Task<User?> GetUserAggregateAsync(int id, CancellationToken ct)
    {
        return await _applicationDbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserAddresses)
            .Include(u => u.UserContacts)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<int> CreateUser(User user, string password)
    {
        _passwordService.SetPassword(user, password);

        _applicationDbContext.Users.Add(user);

        var rows = await _applicationDbContext.SaveChangesAsync();

        return rows;
    }

    public async Task<int> UpdateUser(User user)
    {
        _applicationDbContext.Users.Update(user);

        var rows = await _applicationDbContext.SaveChangesAsync();

        return rows;
    }

    public async Task<bool> EmailExistsAsync(int excludeUserId, string normalizedEmail, CancellationToken ct)
    {
        return await _applicationDbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != excludeUserId &&
                u.NormalizedEmail == normalizedEmail,
                ct);
    }

    public async Task<bool> UserNameExistsAsync(int excludeUserId, string normalizedUserName, CancellationToken ct)
    {
        return await _applicationDbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != excludeUserId &&
                u.NormalizedUserName == normalizedUserName,
                ct);
    }
}
