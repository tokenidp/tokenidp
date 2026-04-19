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
            .When(RequiresAuthorizationCodeRedirect)
            .WithMessage("'Redirect Uri' must not be empty.");

        RuleFor(x => x.RedirectUri)
            .Must(value => string.IsNullOrWhiteSpace(value) || BeAbsoluteUri(value))
            .When(x => !string.IsNullOrWhiteSpace(x.RedirectUri));

        RuleFor(x => x.RedirectUri)
            .Must(BeAbsoluteUri)
            .When(RequiresAuthorizationCodeRedirect)
            .WithMessage("RedirectUri must be a valid absolute URI.");

        RuleFor(x => x.LogoutRedirectUri)
            .Must(value => string.IsNullOrWhiteSpace(value) || BeAbsoluteUri(value))
            .WithMessage("LogoutRedirectUri must be a valid absolute URI.");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500)
            .Must(value => string.IsNullOrWhiteSpace(value) || BeAbsoluteUri(value))
            .WithMessage("IconUrl must be a valid absolute URI.");

        RuleFor(x => x.AccessTokenLifetime)
            .GreaterThan(0);

        RuleFor(x => x.AuthorizationCodeLifetime)
            .GreaterThan(0);

        RuleFor(x => x.RefreshTokenExpiration)
            .GreaterThan(0);

        RuleFor(x => x.RefreshTokenDeliveryMode)
            .IsInEnum();

        RuleFor(x => x.PermitLimit)
            .GreaterThan(0)
            .When(x => x.PermitLimit.HasValue);

        RuleFor(x => x.QueueLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.QueueLimit.HasValue);

        RuleFor(x => x.TimeWindow)
            .Must(value => !value.HasValue || value.Value > TimeSpan.Zero)
            .WithMessage("TimeWindow must be greater than zero.");

        RuleFor(x => x.CibaDefaultExpirySeconds)
            .GreaterThan(0)
            .When(x => x.CibaEnabled);

        RuleFor(x => x.CibaMinIntervalSeconds)
            .GreaterThan(0)
            .When(x => x.CibaEnabled);

        RuleFor(x => x.BackchannelTokenDeliveryMode)
            .Equal(CibaTokenDeliveryModes.Poll)
            .When(x => x.CibaEnabled)
            .WithMessage("Only Poll delivery mode is currently supported.");

        RuleFor(x => x)
            .Must(HaveAtLeastOneCibaHint)
            .When(x => x.CibaEnabled)
            .WithMessage("At least one CIBA user hint type must be enabled.");

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

    private static bool RequiresAuthorizationCodeRedirect(CreateUpdateClient client)
    {
        return (client.GrantTypes ?? new List<GrantTypes>())
            .Contains(GrantTypes.authorization_code);
    }

    private static bool HaveDistinctValues(IEnumerable<string> values)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim());

        return normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() == normalized.Count();
    }

    private static bool HaveAtLeastOneCibaHint(CreateUpdateClient client)
    {
        return client.AllowCibaLoginHint || client.AllowCibaLoginHintToken || client.AllowCibaIdTokenHint;
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
