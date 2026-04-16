namespace TokenIDP.Domain.AggregateRoots.Users;

public class UserContact : Entity<int>
{
    public int UserId { get; private set; }
    public string? Relationship { get; private set; } // e.g., Parent, Spouse, Guardian
    public string ContactType { get; private set; } = null!; // e.g., Email, Mobile, WorkPhone
    public string Email { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public bool IsActive { get; private set; } = true;

    public virtual User User { get; private set; } = default!;

    protected UserContact() { }

    private UserContact(
        string contactType,
        string? relationship,
        string email,
        string phoneNumber,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        bool isActive)
    {
        ContactType = contactType;
        Relationship = relationship;
        Email = email;
        PhoneNumber = phoneNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        IsActive = isActive;
    }

    public static Result Create(
        string contactType,
        string? relationship,
        string? email,
        string? phoneNumber,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        bool isActive,
        out UserContact? contact)
    {
        contact = null;

        var validation = ValidateInput(contactType);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        contact = new UserContact(
            contactType.Trim(),
            relationship?.Trim(),
            email?.Trim() ?? string.Empty,
            phoneNumber?.Trim() ?? string.Empty,
            addressLine1?.Trim(),
            addressLine2?.Trim(),
            city?.Trim(),
            state?.Trim(),
            postalCode?.Trim(),
            country?.Trim(),
            isActive);

        return Result.Success(0);
    }

    private static Result ValidateInput(string contactType)
    {
        return string.IsNullOrWhiteSpace(contactType)
            ? Result.Failure("user.contact.type.invalid", "Contact type cannot be empty.")
            : Result.Success(0);
    }
}
