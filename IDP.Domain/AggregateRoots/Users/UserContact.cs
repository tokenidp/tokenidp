using IDP.Domain.Base;

namespace IDP.Domain.AggregateRoots.Users;

public class UserContact : BaseEntity
{
    [Required]
    public int UserId { get; private set; }
    [MaxLength(50)]
    public string? Relationship { get; private set; } // e.g., Parent, Spouse, Guardian
    [Required, MaxLength(50)]
    public string ContactType { get; private set; } = null!; // e.g., Email, Mobile, WorkPhone
    [MaxLength(256)]
    public string? Email { get; private set; }
    [Required, MaxLength(50)]
    public string? PhoneNumber { get; private set; }
    [MaxLength(200)]
    public string? AddressLine1 { get; private set; }
    [MaxLength(200)]
    public string? AddressLine2 { get; private set; }
    [MaxLength(100)]
    public string? City { get; private set; }

    [MaxLength(100)]
    public string? State { get; private set; }
    [MaxLength(20)]
    public string? PostalCode { get; private set; }
    [MaxLength(100)]
    public string? Country { get; private set; }

    [Required]
    public bool IsActive { get; private set; } = true;

    public virtual User User { get; private set; }

    protected UserContact() { }

    private UserContact(
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
            email?.Trim(),
            phoneNumber?.Trim(),
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