namespace Admin.Core.Users;

public class UserDetail
{
    internal static Expression<Func<User, UserDetail>> Projection =>
    user => new UserDetail()
    {
        Id = user.Id,
        TenantId = user.TenantId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Status = user.StatusId.ToString(),
        UserName = user.UserName,
        NormalizedUserName = user.NormalizedUserName,
        Phone = user.PhoneNumber,
        EmailConfirmed = user.EmailConfirmed,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        TwoFactorEnabled = user.TwoFactorEnabled,
        LockoutEnabled = user.LockoutEnabled,
        AccessFailedCount = user.AccessFailedCount,
        LockoutEnd = user.LockoutEnd,
        SecurityStamp = user.SecurityStamp,
        ConcurrencyStamp = user.ConcurrencyStamp,
        UserCode = user.UserCode,
        Roles = user.UserRoles.Select(s => s.RoleId).ToArray(),
        Addresses = user.UserAddresses
            .Select(address => new UserAddressDetail
            {
                AddressType = address.AddressType.ToString(),
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsActive = address.IsActive
            })
            .ToList(),
        Contacts = user.UserContacts
            .Select(contact => new UserContactDetail
            {
                ContactType = contact.ContactType,
                Relationship = contact.Relationship,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                AddressLine1 = contact.AddressLine1,
                AddressLine2 = contact.AddressLine2,
                City = contact.City,
                State = contact.State,
                PostalCode = contact.PostalCode,
                Country = contact.Country,
                IsActive = contact.IsActive
            })
            .ToList()
    };

    public int Id { get; set; }
    public int TenantId { get; set; }
    public string? Password { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Phone { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string UserCode { get; set; } = default!;
    public int[] Roles { get; set; } = new int[0];
    public List<UserAddressDetail> Addresses { get; set; } = new();
    public List<UserContactDetail> Contacts { get; set; } = new();
}

public class UserAddressDetail
{
    public required string AddressType { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UserContactDetail
{
    public string ContactType { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;
}