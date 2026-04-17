using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Core.Admin.Users;
using TokenIDP.Infrastructure.Projections;

namespace TokenIDP.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAppLogger<UserRepository> _logger;
    private readonly PasswordService _passwordService;

    public UserRepository(IAppLogger<UserRepository> logger,
        ApplicationDbContext dbContext,
        PasswordService passwordService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<User> GetUserById(int id)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        return user!;
    }

    public async Task<UserShortInfo> GetUserShortInfo(int id)
    {
        var user = await _dbContext.Users
            .Where(u => u.Id == id && !u.IsDeleted)
            .AsNoTracking()
            .Select(UserProjection.Projection)
            .FirstOrDefaultAsync();

        return user!;
    }

    public async Task<User?> GetUserAggregateAsync(int id, int tenantId, CancellationToken ct)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserAddresses)
            .Include(u => u.UserContacts)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && !u.IsDeleted, ct);
    }

    public Task<UserDetail?> GetUserDetailAsync(int tenantId, int userId, CancellationToken ct)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted)
            .Select(UserDetail.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaginatedList<UserSearchResult>> SearchUsersAsync(int tenantId, SearchData request, CancellationToken ct)
    {
        var query = _dbContext.UsersSearch
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(user =>
                (user.FullName ?? string.Empty).ToLower().Contains(term) ||
                (user.UserName ?? string.Empty).ToLower().Contains(term) ||
                (user.Email ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await query
            .Select(UserSearchResult.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }

    public async Task<UserLookups> GetUserLookupsAsync(int tenantId, CancellationToken ct)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsDeleted)
            .Select(r => new LookupItem
            {
                Key = r.Id.ToString(),
                Value = r.Name ?? string.Empty
            })
            .ToListAsync(ct);

        return new UserLookups
        {
            Roles = roles,
            UserStatuses = UserLookupMapper.MapUserStatuses(),
            AddressTypes = UserLookupMapper.MapAddressTypes()
        };
    }

    public async Task<IReadOnlyList<PermissionInfo>> GetUserPermissionsAsync(int userId, CancellationToken ct)
    {
        return await _dbContext.UserRolePermissions
            .Where(c => c.UserId == userId)
            .Select(c => new PermissionInfo(
                c.Id,
                c.ParentId,
                c.UserId,
                c.Sequence,
                c.PermissionName,
                c.IsAllowed ? "true" : "false",
                c.Permissionkey,
                c.Icon,
                c.AccessUrl,
                c.RoleName,
                c.ControlType))
            .ToListAsync(ct);
    }

    public async Task<int> CreateUser(User user, string password)
    {
        _passwordService.SetPassword(user, password);

        _dbContext.Users.Add(user);

        var rows = await _dbContext.SaveChangesAsync();

        return rows;
    }

    public async Task<int> UpdateUser(User user)
    {
        _dbContext.Users.Update(user);

        var rows = await _dbContext.SaveChangesAsync();

        return rows;
    }

    public async Task<int> DeleteAsync(User user, CancellationToken ct)
    {
        var deleteResult = user.SoftDelete();
        if (!deleteResult.IsSuccess)
        {
            throw new InvalidOperationException(
                string.Join("; ", deleteResult.Errors.Select(x => x.Message)));
        }

        _dbContext.Users.Update(user);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<User?> GetByTenantAndEmailAsync(int tenantId, string email, CancellationToken ct)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.Email == email && !u.IsDeleted,
                ct);
    }

    public async Task<User?> FindByLoginHintAsync(int tenantId, string loginHint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(loginHint))
        {
            return null;
        }

        var normalizedHint = loginHint.Trim().ToUpperInvariant();
        var isUserId = int.TryParse(loginHint, out var userId);

        return await _dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.TenantId == tenantId &&
                !u.IsDeleted &&
                (
                    u.NormalizedEmail == normalizedHint ||
                    u.NormalizedUserName == normalizedHint ||
                    u.UserCode == loginHint ||
                    (isUserId && u.Id == userId)
                ),
                ct);
    }

    public async Task<User?> GetByTenantAsync(int userId, int tenantId, CancellationToken ct)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted,
                ct);
    }

    public async Task<bool> EmailExistsAsync(int tenantId, int excludeUserId, string normalizedEmail, CancellationToken ct)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.TenantId == tenantId &&
                u.Id != excludeUserId &&
                u.NormalizedEmail == normalizedEmail,
                ct);
    }

    public async Task<bool> UserNameExistsAsync(int tenantId, int excludeUserId, string normalizedUserName, CancellationToken ct)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.TenantId == tenantId &&
                u.Id != excludeUserId &&
                u.NormalizedUserName == normalizedUserName,
                ct);
    }

    public async Task CreatePasswordResetAsync(User user, PasswordResetToken resetToken, CancellationToken ct)
    {
        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(byte[] tokenHash, DateTime nowUtc, CancellationToken ct)
    {
        return await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > nowUtc,
                ct);
    }

    public async Task CreateEmailConfirmationAsync(EmailConfirmationToken confirmationToken, CancellationToken ct)
    {
        _dbContext.EmailConfirmationTokens.Add(confirmationToken);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<EmailConfirmationToken?> GetValidEmailConfirmationTokenAsync(byte[] tokenHash, DateTime nowUtc, CancellationToken ct)
    {
        return await _dbContext.EmailConfirmationTokens
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > nowUtc,
                ct);
    }
}


