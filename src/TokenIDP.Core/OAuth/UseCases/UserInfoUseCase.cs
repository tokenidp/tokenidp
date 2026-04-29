using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class UserInfoUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _identityStore;
    private readonly IAppLogger<UserInfoUseCase> _logger;

    private IReadOnlySet<string> _supportedScopes;

    public UserInfoUseCase(IAppLogger<UserInfoUseCase> logger,
        ICurrentUserService currentUserService,
        IUserRepository identityStore)
    {
        _supportedScopes = StandardScopes.Supported;

        _logger = logger;
        _currentUserService = currentUserService;
        _identityStore = identityStore;
    }

    internal async Task<UserInfo> HandleAsync(CancellationToken cancellationToken = default)
    {
        var scopes = new HashSet<string>(StringComparer.Ordinal);

        AddScopes(scopes, _currentUserService.Scopes);

        var scopeValidation = ValidateScopes(scopes);

        if (!scopeValidation.IsValid)
        {
            throw new InvalidOperationException(scopeValidation.Description);
        }

        var subject = _currentUserService.UserId.ToString();

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new NotFoundException("Missing subject claim.");
        }

        var userInfo = await BuildUserInfoResponse(scopes, subject);

        return userInfo;
    }

    private void AddScopes(HashSet<string> scopes, string value)
    {
        foreach (var scope in value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            scopes.Add(scope);
        }
    }

    private ScopeValidationResult ValidateScopes(HashSet<string> scopes)
    {
        if (scopes.Count == 0)
        {
            return ScopeValidationResult.Insufficient(
                "openid",
                "The access token does not include any scopes.");
        }

        if (!scopes.Contains("openid"))
        {
            return ScopeValidationResult.Insufficient(
                "openid",
                "The access token must include the 'openid' scope.");
        }

        var invalidScopes = scopes
            .Where(scope => !_supportedScopes.Contains(scope))
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            return ScopeValidationResult.Invalid(
                $"Unsupported scope(s): {string.Join(" ", invalidScopes)}.");
        }

        return ScopeValidationResult.Valid();
    }

    private async Task<UserInfo> BuildUserInfoResponse(HashSet<string> scopes, string subject)
    {
        var user = await _identityStore.GetUserShortInfo(Convert.ToInt32(subject));

        if (user == null)
        {
            _logger.LogWarning("User not found with username or email: {UserId}", subject);

            throw new NotFoundException(string.Format("User not found with username or email: {UserId}", subject));
        }

        var userInfo = UserInfo.FromUser(user);

        return userInfo;
    }

    private sealed record ScopeValidationResult(
        bool IsValid,
        int StatusCode,
        string Error,
        string Description,
        string? Scope)
    {
        public static ScopeValidationResult Valid()
        {
            return new ScopeValidationResult(true, StatusCodes.Status200OK, "", "", null);
        }

        public static ScopeValidationResult Invalid(string description)
        {
            return new ScopeValidationResult(
                false,
                StatusCodes.Status400BadRequest,
                "invalid_scope",
                description,
                null);
        }

        public static ScopeValidationResult Insufficient(string scope, string description)
        {
            return new ScopeValidationResult(
                false,
                StatusCodes.Status403Forbidden,
                "insufficient_scope",
                description,
                scope);
        }
    }
}


