using FluentValidation;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;

namespace TokenIDP.Core.Admin.Users.UseCases;

public sealed class CreateAccountUseCase
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICodeSequenceGenerator _userCodeGenerator;
    private readonly PasswordService _passwordService;
    private readonly UserNormalizationService _userNormalizationService;
    private readonly ILookupNormalizer _normalizer;
    private readonly IAppLogger<CreateAccountUseCase> _logger;
    private readonly EmailConfirmationUseCase _emailConfirmationUseCase;
    private readonly ITenantRepository _tenantRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateAccountRequest> _validator;

    public CreateAccountUseCase(
        ITenantContextAccessor tenantContextAccessor,
        ICodeSequenceGenerator userCodeGenerator,
        PasswordService passwordService,
        UserNormalizationService userNormalizationService,
        ILookupNormalizer normalizer,
        IAppLogger<CreateAccountUseCase> logger,
        EmailConfirmationUseCase emailConfirmationUseCase,
        ITenantRepository tenantRepository,
        IClientRepository clientRepository,
        IUserRepository userRepository,
        IValidator<CreateAccountRequest> validator)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _userCodeGenerator = userCodeGenerator;
        _passwordService = passwordService;
        _userNormalizationService = userNormalizationService;
        _normalizer = normalizer;
        _logger = logger;
        _emailConfirmationUseCase = emailConfirmationUseCase;
        _tenantRepository = tenantRepository;
        _clientRepository = clientRepository;
        _userRepository = userRepository;
        _validator = validator;
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

        var validationError = await ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = request.Email.Trim();
        var userName = request.UserName.Trim();
        var phoneNumber = request.PhoneNumber.Trim();
        var password = request.Password;

        var tenantAuth = await _tenantRepository.GetTenantAuthSettingAsync(tenantId, cancellationToken);

        if (tenantAuth is null)
        {
            return CreateAccountResult.Failure(
                "signup.tenant_auth.not_found",
                "Tenant authentication settings were not found.");
        }

        var clientAuthPolicy = await _clientRepository.GetClientAuthPolicy(clientId);

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

        var emailExists = await _userRepository.EmailExistsAsync(
                tenantId,
                0,
                normalizedEmail,
                cancellationToken)
            || await _userRepository.UserNameExistsAsync(
                tenantId,
                0,
                normalizedUserName,
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

        await _userRepository.CreateUser(user, password);

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

    private async Task<CreateAccountResult?> ValidateRequestAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        var firstError = validationResult.Errors.FirstOrDefault();

        if (firstError is not null)
        {
            return CreateAccountResult.Failure(
                firstError.ErrorCode ?? "signup.invalid",
                firstError.ErrorMessage);
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
