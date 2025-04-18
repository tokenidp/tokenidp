namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public partial class User : IdentityUser<int>
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
    public virtual ICollection<UserRole> AppUserRoles { get; private set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; private set; }
    public virtual ICollection<AuthorizationCode> AuthorizationCodes { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public User() : base() { }
}

public partial class User
{
    public string FullName
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}