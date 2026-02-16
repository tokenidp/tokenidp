namespace IDP.Domain.AggregateRoots.Users;

public class UserAddress : Entity<int>
{
    public int UserId { get; private set; }
    public AddressTypes AddressType { get; private set; }
    public string AddressLine1 { get; private set; } = default!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public virtual User User { get; private set; } = default!;

    protected UserAddress() { }

    private UserAddress(
        AddressTypes addressType,
        string addressLine1,
        string? addressLine2,
        string city,
        string? state,
        string? postalCode,
        string country,
        bool isActive = true)
    {
        AddressType = addressType;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        IsActive = isActive;
    }

    public static Result Create(
        string addressType,
        string addressLine1,
        string? addressLine2,
        string city,
        string? state,
        string? postalCode,
        string country,
        bool isActive,
        out UserAddress? address)
    {
        address = null;

        var validation = ValidateInput(addressType, addressLine1, city, country);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var addressTypeResult = ParseAddressType(addressType);

        if (!addressTypeResult.IsSuccess)
            throw new InvalidOperationException(
                string.Join(", ", addressTypeResult.Errors.Select(e => e.Message)));

        Enum.TryParse<AddressTypes>(addressType, ignoreCase: true, out var parsedAddressType);

        address = new UserAddress(
            parsedAddressType,
            addressLine1.Trim(),
            addressLine2?.Trim(),
            city.Trim(),
            state?.Trim(),
            postalCode?.Trim(),
            country.Trim(),
            isActive);

        return Result.Success(0);
    }

    private static Result ValidateInput(
        string addressType,
        string addressLine1,
        string city,
        string country)
    {
        var validation = ValidateRequired(addressLine1, "user.address.line1.invalid",
                "Address line 1 cannot be empty.")
            .Combine(ValidateRequired(city, "user.address.city.invalid",
                "City cannot be empty."))
            .Combine(ValidateRequired(country, "user.address.country.invalid",
                "Country cannot be empty."));

        if (string.IsNullOrEmpty(addressType.ToString()))
        {
            validation = validation.Combine(Result.Failure(
                "user.address.type.invalid",
                "Address type must be greater than zero."));
        }

        return validation;
    }

    private static Result ParseAddressType(string addressType)
    {
        if (string.IsNullOrWhiteSpace(addressType))
        {
            return Result.Failure(
                "user.address_type.empty",
                "AddressType cannot be empty.");
        }

        if (!Enum.TryParse<AddressTypes>(
                addressType,
                ignoreCase: true,
                out var parsed))
        {
            return Result.Failure(
                "user.address_type.invalid",
                $"Invalid AddressType '{addressType}'.");
        }

        return Result.Success(0);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}