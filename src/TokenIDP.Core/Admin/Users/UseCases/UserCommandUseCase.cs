using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;

namespace TokenIDP.Core.Admin.Users.UseCases;

internal class UserCommandUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IAppLogger<UserCommandUseCase> _logger;
    private readonly ICodeSequenceGenerator _userCodeGenerator;
    private readonly UserNormalizationService _userNormalizationService;
    private readonly ILookupNormalizer _normalizer;

    public UserCommandUseCase(
        ICurrentUserService currentUserService,
        IAppLogger<UserCommandUseCase> logger,
        ICodeSequenceGenerator userCodeGenerator,
        UserNormalizationService userNormalizationService,
        ILookupNormalizer normalizer,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _userCodeGenerator = userCodeGenerator;
        _userNormalizationService = userNormalizationService;
        _normalizer = normalizer;
        _userRepository = userRepository;
    }

    public async Task<ApiResult<int>> CreateUser(
        UserDetail request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userName = request.UserName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;

        _logger.LogDebug("Creating user {UserName} for tenant {TenantId}", userName, _currentUserService.TenantId);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.password.required", "Password is required."));
        }

        var uniquenessFailure = await EnsureUniqueUserAsync(
            userId: 0,
            userName,
            email,
            cancellationToken);
        if (uniquenessFailure is not null)
        {
            return uniquenessFailure;
        }

        var createResult = User.Create(
            _currentUserService.TenantId,
            request.FirstName,
            request.LastName,
            userName,
            email,
            phone,
            _currentUserService.UserId,
            request.Roles,
            out var user);

        if (!createResult.IsSuccess || user == null)
        {
            return FailureFromResult(createResult);
        }

        _userNormalizationService.Normalize(user);
        ApplyUserState(user, request);

        var nextValue = await _userCodeGenerator
            .NextUserCodeAsync(_currentUserService.TenantId, cancellationToken);

        user.GenerateUserCode(nextValue);

        var collectionResult = BuildUserCollections(
            request,
            out var addresses,
            out var contacts);
        if (!collectionResult.IsSuccess)
        {
            return FailureFromResult(collectionResult);
        }

        user.ReplaceAddresses(addresses);
        user.ReplaceContacts(contacts);

        await _userRepository.CreateUser(user, request.Password);

        _logger.LogInfo("User created with Id {UserId}", user.Id);

        return ApiResult<int>.Success(user.Id);
    }

    public async Task<ApiResult<int>> UpdateUser(
        int id,
        UserDetail request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Updating user {UserId}", id);

        var user = await _userRepository.GetUserAggregateAsync(
            id,
            _currentUserService.TenantId,
            cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        var userName = request.UserName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;

        if (!string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("user.username.immutable", "User name cannot be changed."));
        }

        var uniquenessFailure = await EnsureUniqueUserAsync(
            userId: id,
            userName,
            email,
            cancellationToken,
            checkUserName: false);
        if (uniquenessFailure is not null)
        {
            return uniquenessFailure;
        }

        var updateResult = user.UpdateProfile(
            request.FirstName,
            request.LastName,
            user.UserName ?? userName,
            email,
            phone,
            request.Roles);

        if (!updateResult.IsSuccess)
        {
            return FailureFromResult(updateResult);
        }

        _userNormalizationService.Normalize(user);

        var collectionResult = BuildUserCollections(
            request,
            out var addresses,
            out var contacts);
        if (!collectionResult.IsSuccess)
        {
            return FailureFromResult(collectionResult);
        }

        user.ReplaceAddresses(addresses);
        user.ReplaceContacts(contacts);
        ApplyUserState(user, request);

        await _userRepository.UpdateUser(user);

        _logger.LogInfo("User updated {UserId}", id);

        return ApiResult<int>.Success(user.Id);
    }

    public async Task<ApiResult<int>> UpdateUserStatus(
        int id,
        UpdateUserStatus request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating user status for {UserId}", id);

        var user = await _userRepository.GetUserAggregateAsync(
            id,
            _currentUserService.TenantId,
            cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found for status update: {UserId}", id);

            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        user.UpdateStatus(request.Status);

        await _userRepository.UpdateUser(user);

        _logger.LogInfo("User status updated {UserId}", id);

        return ApiResult<int>.Success(user.Id);
    }

    public async Task<ApiResult<int>> DeleteUser(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting user {UserId}", id);

        var user = await _userRepository.GetUserAggregateAsync(
            id,
            _currentUserService.TenantId,
            cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found for delete: {UserId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "User not found for the Id {0}".FormatString(id)));
        }

        await _userRepository.DeleteAsync(user, cancellationToken);

        _logger.LogInfo("User deleted {UserId}", id);

        return ApiResult<int>.Success(user.Id);
    }

    private async Task<ApiResult<int>?> EnsureUniqueUserAsync(
        int userId,
        string userName,
        string email,
        CancellationToken cancellationToken,
        bool checkUserName = true)
    {
        if (checkUserName)
        {
            var normalizedUserName = _normalizer.NormalizeName(userName);
            var userNameExists = await _userRepository.UserNameExistsAsync(
                _currentUserService.TenantId,
                userId,
                normalizedUserName,
                cancellationToken);

            if (userNameExists)
            {
                return ApiResult<int>.Failure(
                    ApiError.Failure("user.username.duplicate", "User name already exists."));
            }
        }

        var normalizedEmail = _normalizer.NormalizeEmail(email);
        var emailExists = await _userRepository.EmailExistsAsync(
            _currentUserService.TenantId,
            userId,
            normalizedEmail,
            cancellationToken);

        return emailExists
            ? ApiResult<int>.Failure(
                ApiError.Failure("user.email.duplicate", "Email already exists."))
            : null;
    }

    private static void ApplyUserState(User user, UserDetail request)
    {
        user.ApplyIdentityFlags(
            request.LockoutEnabled,
            request.TwoFactorEnabled,
            request.EmailConfirmed ?? user.EmailConfirmed,
            request.PhoneNumberConfirmed ?? user.PhoneNumberConfirmed,
            request.AccessFailedCount,
            request.LockoutEnd);

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return;
        }

        if (Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
        {
            user.UpdateStatus(parsedStatus);
        }
    }

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }

    private static Result BuildUserCollections(
        UserDetail request,
        out List<UserAddress> addresses,
        out List<UserContact> contacts)
    {
        var addressResult = BuildAddresses(request.Addresses, out addresses);
        var contactResult = BuildContacts(request.Contacts, out contacts);

        if (addressResult.IsSuccess)
        {
            return contactResult;
        }

        return addressResult.Combine(contactResult);
    }

    private static Result BuildAddresses(
        IEnumerable<UserAddressDetail> addresses,
        out List<UserAddress> mapped)
    {
        mapped = new List<UserAddress>();

        var combined = Result.Success(0);
        foreach (var address in addresses ?? Enumerable.Empty<UserAddressDetail>())
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

        var combined = Result.Success(0);
        foreach (var contact in contacts ?? Enumerable.Empty<UserContactDetail>())
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

