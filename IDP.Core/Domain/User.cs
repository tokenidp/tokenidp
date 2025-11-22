namespace IDP.Core.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
internal partial class User : IdentityUser<int>, IBaseEntity, ITenant, IAggregateRoot, IAuditable
{
    public enum UserStatus
    {
        Active,
        Inactive,
        Terminate
    }

    public int TenantId { get; private set; }
    public UserStatus StatusId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public virtual ICollection<UserAddress> UserAddresses { get; private set; }
    public virtual ICollection<UserContact> UserContacts { get; private set; }
    public virtual ICollection<UserRole> UserRoles { get; private set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; private set; }
    public virtual ICollection<AuthorizationCode> AuthorizationCodes { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public User() : base() { }

    public User(
        int tId,
        string fName,
        string lName,
        string uName,
        string email,
        string phone,
        int createdBy,
        int[] roles) : this()
    {
        UserRoles = new List<UserRole>();

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
        CreatedOn = DateTime.UtcNow;
        CreatedBy = createdBy;

        foreach (var role in roles)
        {
            UserRoles.Add(new UserRole(role));
        }
    }

    public void UpdateUser(string fName,
        string lName,
        string uName,
        string email,
        string phone,
        int updatedBy,
        int[] roles)
    {
        FirstName = fName;
        LastName = lName;
        UserName = uName;
        Email = email;
        PhoneNumber = phone;
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        foreach (var role in roles)
        {
            if (UserRoles.Select(s => s.RoleId).ToList().Contains(role))
                continue;

            UserRoles.Add(new UserRole(role));
        }
    }

    public void UpdateStatus(UserStatus userStatus)
    {
        StatusId = userStatus;
    }

    public void SetCreatedByAndCreatedOn(int userId)
    {
        CreatedOn = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public void SetUpdatedByAndUpdatedOn(int userId)
    {
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}

internal partial class User
{
    public string FullName
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}