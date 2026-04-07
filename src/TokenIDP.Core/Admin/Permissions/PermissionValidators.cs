using FluentValidation;

namespace TokenIDP.Core.Admin.Permissions;

internal sealed class CreateUpdatePermissionValidator : AbstractValidator<CreateUpdatePermission>
{
    public CreateUpdatePermissionValidator()
    {
        RuleFor(x => x.TenantId)
            .GreaterThan(0);

        RuleFor(x => x.PermissionKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.PermissionName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ControlType)
            .NotEmpty();

        RuleFor(x => x.Sequence)
            .GreaterThanOrEqualTo(0);

        RuleForEach(x => x.ChildPermissions)
            .SetValidator(this);
    }
}
