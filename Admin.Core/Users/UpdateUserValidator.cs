using FluentValidation;

namespace Admin.Core.Users;

internal class UpdateUserValidator : AbstractValidator<CreateUpdateUser>
{
    private readonly ApplicationDbContext _context;

    public UpdateUserValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.UserName)
            .MustAsync(BeUniqueUserName).WithMessage("The user name already exists.");
    }

    public async Task<bool> BeUniqueUserName(CreateUpdateUser user, string userName, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(u => u.Id != user.Id)
            .AllAsync(l => l.UserName != userName, cancellationToken);
    }
}