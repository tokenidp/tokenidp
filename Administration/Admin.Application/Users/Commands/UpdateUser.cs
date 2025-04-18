namespace Identity.Application.Users.Commands;

public class UpdateUser : IRequest<Result>
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Phone { get; set; }
    public bool? IsWindowsAuth { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public int? ReportToId { get; set; }
    public int[] Roles { get; set; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUser, Result>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public UpdateUserCommandHandler(ICurrentUserService currentUserService,
        UserManager<AppUser> userManager,
        IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<Result> Handle(UpdateUser request, CancellationToken cancellationToken)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == request.Id);

        if (user == null)
        {
            return Result.Failure("NotFound", "User not found for the Id {0}".FormatString(request.Id));
        }

        user.UpdateUser(
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email,
            request.Phone,
            _currentUserService.UserId,
            request.Roles);

        var result = await _userManager.UpdateAsync(user);

        return result.ToApplicationResult(user.Id);
    }
}
