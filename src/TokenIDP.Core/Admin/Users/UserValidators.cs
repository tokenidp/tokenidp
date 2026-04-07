using FluentValidation;
using System.ComponentModel.DataAnnotations;
using TokenIDP.Core.Admin.Users.UseCases;

namespace TokenIDP.Core.Admin.Users;

internal sealed class UserDetailValidator : AbstractValidator<UserDetail>
{
    public UserDetailValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Roles)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .When(x => x.Id <= 0);

        RuleFor(x => x.Status)
            .Must(BeKnownStatus)
            .WithMessage("Status is invalid.");

        RuleForEach(x => x.Addresses)
            .SetValidator(new UserAddressDetailValidator());

        RuleForEach(x => x.Contacts)
            .SetValidator(new UserContactDetailValidator());
    }

    private static bool BeKnownStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status) || Enum.TryParse<UserStatus>(status, true, out _);
    }
}

internal sealed class UserAddressDetailValidator : AbstractValidator<UserAddressDetail>
{
    public UserAddressDetailValidator()
    {
        RuleFor(x => x.AddressType)
            .NotEmpty();

        RuleFor(x => x.AddressLine1)
            .NotEmpty();

        RuleFor(x => x.City)
            .NotEmpty();

        RuleFor(x => x.Country)
            .NotEmpty();
    }
}

internal sealed class UserContactDetailValidator : AbstractValidator<UserContactDetail>
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public UserContactDetailValidator()
    {
        RuleFor(x => x.ContactType)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HaveEmailOrPhone)
            .WithMessage("A contact requires an email or phone number.");

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailValidator.IsValid(value))
            .WithMessage("Email must be a valid email address.");
    }

    private static bool HaveEmailOrPhone(UserContactDetail contact)
    {
        return !string.IsNullOrWhiteSpace(contact.Email) ||
               !string.IsNullOrWhiteSpace(contact.PhoneNumber);
    }
}

internal sealed class UpdateUserStatusValidator : AbstractValidator<UpdateUserStatus>
{
    public UpdateUserStatusValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}

internal sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.ClientId)
            .NotEmpty();
    }
}

internal sealed class CompletePasswordResetRequestValidator : AbstractValidator<CompletePasswordResetRequest>
{
    public CompletePasswordResetRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}

internal sealed class CompleteEmailConfirmationRequestValidator : AbstractValidator<CompleteEmailConfirmationRequest>
{
    public CompleteEmailConfirmationRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}

internal sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithErrorCode("user.first_name.invalid");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithErrorCode("user.last_name.invalid");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithErrorCode("user.email.invalid");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithErrorCode("user.username.invalid");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithErrorCode("user.phone.invalid");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithErrorCode("user.password.invalid");
    }
}
