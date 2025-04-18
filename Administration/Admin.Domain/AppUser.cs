using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Identity.Domain.Entities;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public partial class AppUser : IdentityUser<int>, IBaseEntity, ITenant, IAggregateRoot, IAuditable
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
    public int? ReportToId { get; private set; }
    public bool? IsWindowsAuth { get; private set; }
    public string Address1 { get; private set; }
    public string Address2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string Zip { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public virtual ICollection<AppUserRole> AppUserRoles { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public AppUser() : base() { }

    public AppUser(
        int tId,
        string fName,
        string lName,
        string uName,
        string email,
        string phone,
        int createdBy,
        int[] roles) : this()
    {
        AppUserRoles = new List<AppUserRole>();

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
            AppUserRoles.Add(new AppUserRole(role));
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
            if (AppUserRoles.Select(s => s.RoleId).ToList().Contains(role))
                continue;

            AppUserRoles.Add(new AppUserRole(role));
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

public partial class AppUser
{
    public string FullName
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}