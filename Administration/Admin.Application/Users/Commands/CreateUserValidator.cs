using FluentValidation;

namespace Identity.Application.Users.Commands;

public class CreateUserValidator : AbstractValidator<CreateUser>
{
    private readonly IApplicationDbContext _context;

    public CreateUserValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.TenantId)
            .NotEqual(0).WithMessage("Tenant Id is required.");

        RuleFor(v => v.UserName)
            .MustAsync(BeUniqueUserName).WithMessage("The user name already exists.");
    }

    public async Task<bool> BeUniqueUserName(string userName, CancellationToken cancellationToken)
    {
        return await _context.AppUsers
            .AllAsync(l => l.UserName != userName, cancellationToken);
    }
}