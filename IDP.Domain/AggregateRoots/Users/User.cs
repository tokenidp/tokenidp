using IDP.Domain.DomainEvents.Users;
using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots.Users;

public partial class User : IdentityUser<int>, IAggregateRoot, ITenant
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<UserAddress> _userAddresses = new();
    private readonly List<UserContact> _userContacts = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public int TenantId { get; private set; }
    public UserStatus StatusId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserAddress> UserAddresses => _userAddresses.AsReadOnly();
    public IReadOnlyCollection<UserContact> UserContacts => _userContacts.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public virtual Tenant Tenant { get; private set; }

    protected User() : base() { }

    private User(
        int tId,
        string fName,
        string lName,
        string uName,
        string email,
        string phone,
        int[] roles) : this()
    {
        TenantId = tId;
        FirstName = fName;
        LastName = lName;
        UserName = uName;
        Email = email;
        PhoneNumber = phone;
        TwoFactorEnabled = false;
        PhoneNumberConfirmed = true;
        LockoutEnabled = false;
        AccessFailedCount = 3;
        EmailConfirmed = true;
        StatusId = UserStatus.Active;

        SyncRoles(roles);
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public Result UpdateProfile(
        string fName,
        string lName,
        string uName,
        string email,
        string phone,
        int[] roles)
    {
        var validation = ValidateInput(fName, lName, uName, email, phone);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        FirstName = fName;
        LastName = lName;
        UserName = uName;
        Email = email;
        PhoneNumber = phone;

        SyncRoles(roles);

        return Result.Success(Id);
    }

    public Result UpdateUser(
        string fName,
        string lName,
        string uName,
        string email,
        string phone,
        int[] roles)
    {
        return UpdateProfile(fName, lName, uName, email, phone, roles);
    }

    public void UpdateStatus(UserStatus userStatus)
    {
        StatusId = userStatus;
    }

    public Result ReplaceAddresses(IEnumerable<UserAddress> addresses)
    {
        if (addresses == null)
        {
            return Result.Success(Id);
        }

        _userAddresses.Clear();
        foreach (var address in addresses)
        {
            _userAddresses.Add(address);
        }

        return Result.Success(Id);
    }

    public Result ReplaceContacts(IEnumerable<UserContact> contacts)
    {
        if (contacts == null)
        {
            return Result.Success(Id);
        }

        _userContacts.Clear();
        foreach (var contact in contacts)
        {
            _userContacts.Add(contact);
        }

        return Result.Success(Id);
    }

    public static Result Create(
        int tenantId,
        string firstName,
        string lastName,
        string userName,
        string email,
        string phone,
        int createdBy,
        int[] roles,
        out User? user)
    {
        user = null;

        var validation = ValidateInput(firstName, lastName, userName, email, phone);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        user = new User(
            tenantId,
            firstName.Trim(),
            lastName.Trim(),
            userName.Trim(),
            email.Trim(),
            phone.Trim(),
            roles);

        return Result.Success(0);
    }

    private void SyncRoles(IEnumerable<int> roles)
    {
        var desiredRoles = new HashSet<int>(roles ?? Array.Empty<int>());

        var toRemove = _userRoles
            .Where(role => !desiredRoles.Contains(role.RoleId))
            .ToList();

        foreach (var role in toRemove)
        {
            _userRoles.Remove(role);
        }

        foreach (var roleId in desiredRoles)
        {
            if (_userRoles.Any(role => role.RoleId == roleId))
            {
                continue;
            }

            _userRoles.Add(new UserRole(roleId));
        }
    }

    private static Result ValidateInput(
        string firstName,
        string lastName,
        string userName,
        string email,
        string phone)
    {
        return ValidateRequired(firstName, "user.first_name.invalid",
                "First name cannot be empty.")
            .Combine(ValidateRequired(lastName, "user.last_name.invalid",
                "Last name cannot be empty."))
            .Combine(ValidateRequired(userName, "user.username.invalid",
                "User name cannot be empty."))
            .Combine(ValidateRequired(email, "user.email.invalid",
                "Email cannot be empty."))
            .Combine(ValidateRequired(phone, "user.phone.invalid",
                "Phone number cannot be empty."));
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}

public partial class User
{
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public string FullName
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }

    public void MarkLoginSuccess(
        Guid correlationId,
        string? ipAddress,
        string? userAgent)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.Login,
            AuthenticationResult.Success,
            "Login successful",
            correlationId,
            ipAddress,
            userAgent);
    }

    public void MarkLoginFailed(
        string reason,
        Guid correlationId,
        string? ipAddress,
        string? userAgent)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.Login,
            AuthenticationResult.Failed,
            reason,
            correlationId,
            ipAddress,
            userAgent);
    }

    public void LockAccount(
        string reason,
        Guid correlationId)
    {
        LockoutEnabled = true;

        RecordAuthenticationEvent(
            AuthenticationAction.Login,
            AuthenticationResult.Locked,
            reason,
            correlationId,
            ipAddress: "system",
            userAgent: "system");
    }

    public void UnlockAccount(Guid correlationId)
    {
        LockoutEnabled = false;

        RecordAuthenticationEvent(
            AuthenticationAction.Login,
            AuthenticationResult.Unlocked,
            "Account unlocked",
            correlationId,
            ipAddress: "system",
            userAgent: "system");
    }

    public void MarkLogout(
        Guid correlationId,
        string? ipAddress,
        string? userAgent)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.Logout,
            AuthenticationResult.Success,
            "User logged out",
            correlationId,
            ipAddress,
            userAgent);
    }

    public void MarkMfaChallengeSent(
        Guid correlationId,
        string? ipAddress)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.MfaChallenge,
            AuthenticationResult.Requested,
            "MFA challenge sent",
            correlationId,
            ipAddress,
            userAgent: "system");
    }

    public void MarkMfaValidated(
        Guid correlationId,
        string? ipAddress)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.MfaChallenge,
            AuthenticationResult.Success,
            "MFA validated",
            correlationId,
            ipAddress,
            userAgent: "system");
    }

    public void MarkPasswordResetRequested(Guid correlationId)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.PasswordReset,
            AuthenticationResult.Requested,
            "Password reset requested",
            correlationId,
            ipAddress: "system",
            userAgent: "system");
    }

    public void MarkPasswordResetCompleted(Guid correlationId)
    {
        RecordAuthenticationEvent(
            AuthenticationAction.PasswordReset,
            AuthenticationResult.Completed,
            "Password reset completed",
            correlationId,
            ipAddress: "system",
            userAgent: "system");
    }

    public void SetCreated(int userId)
    {
        CreatedOn = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public void SetUpdated(int userId)
    {
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }

    private void RecordAuthenticationEvent(
        AuthenticationAction action,
        AuthenticationResult result,
        string? description,
        Guid correlationId,
        string? ipAddress,
        string? userAgent)
    {
        var evt = new AuthenticationFlowEvent(
            UserId: Id,
            TenantId: TenantId,
            Action: action,
            Result: result,
            Description: description!,
            CorrelationId: correlationId,
            IpAddress: ipAddress,
            UserAgent: userAgent
        );

        AddDomainEvent(evt);
    }
}

