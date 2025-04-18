using FluentValidation;

namespace Identity.Application.Users.Commands;

public class UpdateUserValidator : AbstractValidator<UpdateUser>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.UserName)
            .MustAsync(BeUniqueUserName).WithMessage("The user name already exists.");
    }

    public async Task<bool> BeUniqueUserName(UpdateUser user, string userName, CancellationToken cancellationToken)
    {
        return await _context.AppUsers
            .Where(u => u.Id != user.Id)
            .AllAsync(l => l.UserName != userName, cancellationToken);
    }
}