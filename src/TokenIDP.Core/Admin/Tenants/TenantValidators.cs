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

        RuleFor(x => x.TenantKey)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Tenant key must contain lowercase letters, numbers, and hyphens only.");

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailValidator.IsValid(value))
            .WithMessage("Email must be a valid email address.");

        When(x => x.Id == 0, () =>
        {
            RuleFor(x => ResolveAdminEmail(x))
                .NotEmpty()
                .Must(EmailValidator.IsValid)
                .WithMessage("Admin email must be a valid email address.");

            RuleFor(x => x.AdminFirstName)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.AdminLastName)
                .NotEmpty()
                .MaximumLength(50);
        });

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

    private static string? ResolveAdminEmail(CreateUpdateTenant tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            return tenant.AdminEmail;
        }

        return tenant.Email;
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

internal sealed class UpdateTenantSocialProviderValidator : AbstractValidator<UpdateTenantSocialProvider>
{
    public UpdateTenantSocialProviderValidator()
    {
        When(x => x.Enabled, () =>
        {
            RuleFor(x => x.ClientId)
                .NotEmpty();

            RuleFor(x => x.Scopes)
                .NotEmpty();
        });
    }
}
