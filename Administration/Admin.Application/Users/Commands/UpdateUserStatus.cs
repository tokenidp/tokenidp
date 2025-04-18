using static Identity.Domain.Entities.AppUser;

namespace Identity.Application.Users.Commands;

public class UpdateUserStatus : IRequest<Result>
{
    public int Id { get; set; }
    public UserStatus Status { get; set; }
}

[SuppressMessage("SonarLint", "S4487", Justification = "_currentUserService will use in future")]
public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatus, Result>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public UpdateUserStatusCommandHandler(ICurrentUserService currentUserService,
        UserManager<AppUser> userManager,
        IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<Result> Handle(UpdateUserStatus request, CancellationToken cancellationToken)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == request.Id);

        if (user == null)
        {
            return Result.Failure("NotFound", "User not found for the Id {0}".FormatString(request.Id));
        }

        user.UpdateStatus(request.Status);

        var result = await _userManager.UpdateAsync(user);

        return result.ToApplicationResult(user.Id);
    }
}