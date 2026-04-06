using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.Admin.Users.UseCases;

internal class UserCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStore _userStore;
    private readonly IAppLogger<UserCommandUseCase> _logger;
    private readonly ICodeSequenceGenerator _userCodeGenerator;
    private readonly UserNormalizationService _userNormalizationService;
    private readonly ILookupNormalizer _normalizer;

    public UserCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<UserCommandUseCase> logger,
        ICodeSequenceGenerator userCodeGenerator,
        UserNormalizationService userNormalizationService,
        ILookupNormalizer normalizer,
        IUserStore userStore)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _userCodeGenerator = userCodeGenerator;
        _userNormalizationService = userNormalizationService;
        _normalizer = normalizer;
        _userStore = userStore;
    }

    public async Task<ApiResult<int>> CreateUser(
        UserDetail request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating user {UserName} for tenant {TenantId}", request.UserName, request.TenantId);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.password.required", "Password is required."));
        }

        var userName = request.UserName.Trim();
        var email = request.Email?.Trim() ?? string.Empty;
        var normalizedUserName = _normalizer.NormalizeName(userName);
        var normalizedEmail = _normalizer.NormalizeEmail(email);

        var userNameExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.TenantId == _currentUserService.TenantId &&
                     (u.NormalizedUserName == normalizedUserName || u.UserName == userName),
                cancellationToken);

        if (userNameExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.username.duplicate", "User name already exists."));
        }

        var emailExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.TenantId == _currentUserService.TenantId &&
                     (u.NormalizedEmail == normalizedEmail || u.Email == email),
                cancellationToken);

        if (emailExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.email.duplicate", "Email already exists."));
        }

        var createResult = User.Create(
            _currentUserService.TenantId,
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email,
            request.Phone!,
            _currentUserService.UserId,
            request.Roles,
            out var user);

        if (!createResult.IsSuccess || user == null)
        {
            return FailureFromResult(createResult);
        }

        _userNormalizationService.Normalize(user);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
        {
            user.UpdateStatus(parsedStatus);
        }

        var nextValue = await _userCodeGenerator
            .NextUserCodeAsync(_currentUserService.TenantId, cancellationToken);

        user.GenerateUserCode(nextValue);

        user.ApplyIdentityFlags(
            request.LockoutEnabled,
            request.TwoFactorEnabled,
            request.EmailConfirmed,
            request.PhoneNumberConfirmed,
            request.AccessFailedCount,
            request.LockoutEnd);

        var addressResult = BuildAddresses(request.Addresses, out var addresses);
        if (!addressResult.IsSuccess)
        {
            return FailureFromResult(addressResult);
        }

        var contactResult = BuildContacts(request.Contacts, out var contacts);
        if (!contactResult.IsSuccess)
        {
            return FailureFromResult(contactResult);
        }

        user.ReplaceAddresses(addresses);
        user.ReplaceContacts(contacts);

        await _userStore.CreateUser(user, request.Password);

        _logger.LogInfo("User created with Id {UserId}", user.Id);

        return ApiResult<int>.Success(user.Id);
    }

    public async Task<ApiResult<int>> UpdateUser(
        int id,
        UserDetail request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating user {UserId}", id);

        var user = await _userStore.GetUserAggregateAsync(id, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.username.immutable", "User name cannot be changed."));
        }

        var email = request.Email.Trim();
        var normalizedEmail = _normalizer.NormalizeEmail(email);
        var emailExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.TenantId == _currentUserService.TenantId &&
                     u.Id != id &&
                     (u.NormalizedEmail == normalizedEmail || u.Email == email),
                cancellationToken);

        if (emailExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.email.duplicate", "Email already exists."));
        }

        var updateResult = user.UpdateProfile(
            request.FirstName,
            request.LastName,
            user.UserName ?? request.UserName,
            request.Email,
            request.Phone,
            request.Roles);

        if (!updateResult.IsSuccess)
        {
            return FailureFromResult(updateResult);
        }

        _userNormalizationService.Normalize(user);

        var addressResult = BuildAddresses(request.Addresses, out var addresses);
        if (!addressResult.IsSuccess)
        {
            return FailureFromResult(addressResult);
        }

        var contactResult = BuildContacts(request.Contacts, out var contacts);
        if (!contactResult.IsSuccess)
        {
            return FailureFromResult(contactResult);
        }

        user.ReplaceAddresses(addresses);
        user.ReplaceContacts(contacts);
        user.ApplyIdentityFlags(
            request.LockoutEnabled,
            request.TwoFactorEnabled,
            request.EmailConfirmed,
            request.PhoneNumberConfirmed,
            request.AccessFailedCount,
            request.LockoutEnd);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
        {
            user.UpdateStatus(parsedStatus);
        }

        await _userStore.UpdateUser(user);

        _logger.LogInfo("User updated {UserId}", id);

        return ApiResult<int>.Success(user.Id);
    }

    public async Task<ApiResult<int>> UpdateUserStatus(
        int id,
        UpdateUserStatus request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating user status for {UserId}", id);

        var user = await _userStore.GetUserById(id);

        if (user == null)
        {
            _logger.LogWarning("User not found for status update: {UserId}", id);

            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        user.UpdateStatus(request.Status);

        await _userStore.UpdateUser(user);

        _logger.LogInfo("User status updated {UserId}", id);

        return ApiResult<int>.Success(user.Id);
    }

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }

    private static Result BuildAddresses(
        IEnumerable<UserAddressDetail> addresses,
        out List<UserAddress> mapped)
    {
        mapped = new List<UserAddress>();

        if (addresses == null)
        {
            return Result.Success(0);
        }

        var combined = Result.Success(0);
        foreach (var address in addresses)
        {
            var createResult = UserAddress.Create(
                address.AddressType,
                address.AddressLine1,
                address.AddressLine2,
                address.City,
                address.State,
                address.PostalCode,
                address.Country,
                address.IsActive,
                out var created);

            if (!createResult.IsSuccess)
            {
                combined = combined.Combine(createResult);
                continue;
            }

            if (created != null)
            {
                mapped.Add(created);
            }
        }

        return combined;
    }

    private static Result BuildContacts(
        IEnumerable<UserContactDetail> contacts,
        out List<UserContact> mapped)
    {
        mapped = new List<UserContact>();

        if (contacts == null)
        {
            return Result.Success(0);
        }

        var combined = Result.Success(0);
        foreach (var contact in contacts)
        {
            var createResult = UserContact.Create(
                contact.ContactType,
                contact.Relationship,
                contact.Email,
                contact.PhoneNumber,
                contact.AddressLine1,
                contact.AddressLine2,
                contact.City,
                contact.State,
                contact.PostalCode,
                contact.Country,
                contact.IsActive,
                out var created);

            if (!createResult.IsSuccess)
            {
                combined = combined.Combine(createResult);
                continue;
            }

            if (created != null)
            {
                mapped.Add(created);
            }
        }

        return combined;
    }
}
