using FluentValidation;

namespace TokenIDP.Core.Admin.Clients;

internal sealed class CreateUpdateClientValidator : AbstractValidator<CreateUpdateClient>
{
    public CreateUpdateClientValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.RedirectUri)
            .NotEmpty()
            .Must(BeAbsoluteUri)
            .WithMessage("RedirectUri must be a valid absolute URI.");

        RuleFor(x => x.LogoutRedirectUri)
            .Must(value => string.IsNullOrWhiteSpace(value) || BeAbsoluteUri(value))
            .WithMessage("LogoutRedirectUri must be a valid absolute URI.");

        RuleFor(x => x.AccessTokenLifetime)
            .GreaterThan(0);

        RuleFor(x => x.AuthorizationCodeLifetime)
            .GreaterThan(0);

        RuleFor(x => x.RefreshTokenExpiration)
            .GreaterThan(0);

        RuleFor(x => x.TwoFactorCodeExpiry)
            .GreaterThan(0)
            .When(x => x.TwoFactorEnabled);

        RuleFor(x => x.GrantTypes)
            .NotEmpty();

        RuleFor(x => x.AuthPolicy)
            .NotNull();

        RuleFor(x => x.Scopes)
            .Must(HaveDistinctValues)
            .WithMessage("Scopes cannot contain duplicate values.");

        RuleFor(x => x.ApiResources)
            .Must(HaveDistinctValues)
            .WithMessage("ApiResources cannot contain duplicate values.");

        RuleFor(x => x.ExternalProviders)
            .Must(values => values.Distinct().Count() == values.Count)
            .WithMessage("ExternalProviders cannot contain duplicate values.");
    }

    private static bool BeAbsoluteUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static bool HaveDistinctValues(IEnumerable<string> values)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim());

        return normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() == normalized.Count();
    }
}

internal sealed class RotateClientSecretRequestValidator : AbstractValidator<RotateClientSecretRequest>
{
    public RotateClientSecretRequestValidator()
    {
        RuleFor(x => x.ClientSecretExpiry)
            .GreaterThan(0)
            .When(x => x.ClientSecretExpiry.HasValue);
    }
}
