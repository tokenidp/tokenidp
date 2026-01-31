namespace IDP.Domain.AggregateRoots.Users;

public class UserShortInfo
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string UserName { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public bool EmailConfirmed { get; private set; }
    public string PhoneNumber { get; private set; } = default!;
    public bool PhoneNumberConfirmed { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    private UserShortInfo() { }

    public UserShortInfo(int id,
        int tenantId,
        string fullName,
        string email,
        bool emailConfirmed,
        string userName,
        string firstName,
        string lastName,
        string phoneNumber,
        bool phoneNumberVerified,
        DateTime createdOn,
        DateTime? updatedOn)
    {
        Id = id;
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
        EmailConfirmed = emailConfirmed;
        UserName = userName;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        PhoneNumberConfirmed = phoneNumberVerified;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;

    }
}
