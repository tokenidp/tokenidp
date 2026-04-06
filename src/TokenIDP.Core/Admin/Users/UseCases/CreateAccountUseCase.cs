using TokenIDP.Core.Admin.Common;
using System.ComponentModel.DataAnnotations;

namespace TokenIDP.Core.Admin.Users.UseCases;

public sealed class CreateAccountUseCase
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICodeSequenceGenerator _userCodeGenerator;
    private readonly PasswordService _passwordService;
    private readonly UserNormalizationService _userNormalizationService;
    private readonly ILookupNormalizer _normalizer;
    private readonly IAppLogger<CreateAccountUseCase> _logger;
    private readonly EmailConfirmationUseCase _emailConfirmationUseCase;

    public CreateAccountUseCase(
        IApplicationDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        ICodeSequenceGenerator userCodeGenerator,
        PasswordService passwordService,
        UserNormalizationService userNormalizationService,
        ILookupNormalizer normalizer,
        IAppLogger<CreateAccountUseCase> logger,
        EmailConfirmationUseCase emailConfirmationUseCase)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
        _userCodeGenerator = userCodeGenerator;
        _passwordService = passwordService;
        _userNormalizationService = userNormalizationService;
        _normalizer = normalizer;
        _logger = logger;
        _emailConfirmationUseCase = emailConfirmationUseCase;
    }

    public async Task<CreateAccountResult> Execute(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantContextAccessor.TenantId;
        var clientId = _tenantContextAccessor.ClientId;

        if (tenantId <= 0)
        {
            return CreateAccountResult.Failure(
                "signup.tenant.invalid",
                "Tenant context is missing.");
        }

        if (clientId <= 0)
        {
            return CreateAccountResult.Failure(
                "signup.client.invalid",
                "Client context is missing.");
        }

        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var userName = request.UserName?.Trim() ?? string.Empty;
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        var validationError = ValidateRequest(firstName, lastName, email, userName, phoneNumber, password);
        if (validationError is not null)
        {
            return validationError;
        }

        var tenantAuth = await _dbContext.TenantAuthSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (tenantAuth is null)
        {
            return CreateAccountResult.Failure(
                "signup.tenant_auth.not_found",
                "Tenant authentication settings were not found.");
        }

        var clientAuthPolicy = await _dbContext.ClientAuthPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken);

        if (clientAuthPolicy is null ||
            !tenantAuth.AllowSelfRegistration ||
            !clientAuthPolicy.AllowSelfRegistrationOverride)
        {
            return CreateAccountResult.Failure(
                "signup.not_allowed",
                "Self-registration is not enabled for this client.");
        }

        if (clientAuthPolicy.DefaultRoleId is null)
        {
            return CreateAccountResult.Failure(
                "signup.default_role",
                "Default role is not enabled for this client.");
        }

        var normalizedEmail = _normalizer.NormalizeEmail(email);
        var normalizedUserName = _normalizer.NormalizeName(userName);

        var emailExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     ((x.NormalizedEmail == normalizedEmail || x.Email == email)
                     || (x.NormalizedUserName == normalizedUserName || x.UserName == userName)),
                cancellationToken);

        if (emailExists)
        {
            return CreateAccountResult.Failure(
                "user.email.duplicate",
                "Email or User name already exists.");
        }

        var createResult = User.Create(
            tenantId,
            firstName,
            lastName,
            userName,
            email,
            phoneNumber,
            createdBy: 0,
            roles: new[] { clientAuthPolicy.DefaultRoleId ?? 0 },
            out var user);

        if (!createResult.IsSuccess || user is null)
        {
            return FailureFromResult(createResult);
        }

        _userNormalizationService.Normalize(user);

        user.ApplyIdentityFlags(
            lookoutEnabled: false,
            twoFactorEnabled: false,
            emailConfirmed: !tenantAuth.RequireEmailVerification,
            phoneNumberConfirmed: false,
            accessFailedCount: 0,
            lookoutEnd: null);

        var nextValue = await _userCodeGenerator.NextUserCodeAsync(tenantId, cancellationToken);
        user.GenerateUserCode(nextValue);

        _passwordService.SetPassword(user, password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (tenantAuth.RequireEmailVerification)
        {
            await _emailConfirmationUseCase.InitiateEmailConfirmation(
                new InitiateEmailConfirmationCommand
                {
                    UserId = user.Id,
                    AuthorizationContextId = request.AuthorizationContextId
                },
                cancellationToken);
        }

        _logger.LogInfo(
            "Public account created for tenant {TenantId} and client {ClientId} with user {UserId}",
            tenantId,
            clientId,
            user.Id);

        return CreateAccountResult.Success(user.Id, tenantAuth.RequireEmailVerification);
    }

    private static CreateAccountResult? ValidateRequest(
        string firstName,
        string lastName,
        string email,
        string userName,
        string phoneNumber,
        string password)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return CreateAccountResult.Failure("user.first_name.invalid", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return CreateAccountResult.Failure("user.last_name.invalid", "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return CreateAccountResult.Failure("user.email.invalid", "Email is required.");
        }

        if (!EmailValidator.IsValid(email))
        {
            return CreateAccountResult.Failure("user.email.invalid", "Invalid email.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return CreateAccountResult.Failure("user.username.invalid", "User name is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return CreateAccountResult.Failure("user.phone.invalid", "Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return CreateAccountResult.Failure("user.password.required", "Password is required.");
        }

        if (password.Length < 8)
        {
            return CreateAccountResult.Failure("user.password.invalid", "Password must be at least 8 characters.");
        }

        return null;
    }

    private static CreateAccountResult FailureFromResult(Result result)
    {
        var firstError = result.Errors.FirstOrDefault();

        return CreateAccountResult.Failure(
            firstError?.Code ?? "signup.invalid",
            firstError?.Message ?? "Unable to create account.");
    }
}

public sealed class CreateAccountRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AuthorizationContextId { get; set; } = string.Empty;
}

public sealed class CreateAccountResult
{
    public bool IsSuccess { get; private set; }
    public int? UserId { get; private set; }
    public bool RequiresEmailVerification { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    private CreateAccountResult() { }

    public static CreateAccountResult Success(int userId, bool requiresEmailVerification)
    {
        return new CreateAccountResult
        {
            IsSuccess = true,
            UserId = userId,
            RequiresEmailVerification = requiresEmailVerification
        };
    }

    public static CreateAccountResult Failure(string errorCode, string errorMessage)
    {
        return new CreateAccountResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
