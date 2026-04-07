using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace TokenIDP.Core.Admin.Tenants;

internal sealed class CreateUpdateTenantValidator : AbstractValidator<CreateUpdateTenant>
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public CreateUpdateTenantValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailValidator.IsValid(value))
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.AuthSettings)
            .NotNull();

        When(x => x.AuthSettings is not null, () =>
        {
            RuleFor(x => x.AuthSettings.TwoFactorCodeExpiry)
                .GreaterThan(0)
                .When(x => x.AuthSettings.TwoFactorEnabled);
        });

        RuleForEach(x => x.Providers)
            .SetValidator(new TenantExternalProviderDetailValidator());
    }
}

internal sealed class TenantExternalProviderDetailValidator : AbstractValidator<TenantExternalProviderDetail>
{
    public TenantExternalProviderDetailValidator()
    {
        When(x => x.Enabled, () =>
        {
            RuleFor(x => x.ClientId)
                .NotEmpty();
        });
    }
}
