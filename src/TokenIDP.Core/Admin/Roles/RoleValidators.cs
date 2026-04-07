using FluentValidation;

namespace TokenIDP.Core.Admin.Roles;

internal sealed class CreateUpdateRoleValidator : AbstractValidator<CreateUpdateRole>
{
    public CreateUpdateRoleValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.RoleDescription)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.RolePermissions)
            .NotNull();

        RuleForEach(x => x.RolePermissions)
            .SetValidator(new CreateUpdateRolePermissionValidator());
    }
}

internal sealed class CreateUpdateRolePermissionValidator : AbstractValidator<CreateUpdateRolePermission>
{
    public CreateUpdateRolePermissionValidator()
    {
        RuleFor(x => x.PermissionId)
            .GreaterThan(0);

        RuleFor(x => x.PermissionKey)
            .NotEmpty();
    }
}
