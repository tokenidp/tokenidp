namespace Identity.Application.Users.Commands;

public class CreateUser : IRequest<Result>
{
    public int TenantId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Phone { get; set; }
    public string Password { get; set; }
    public bool? IsWindowsAuth { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public int? ReportToId { get; set; }
    public int[] Roles { get; set; }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUser, Result>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public CreateUserCommandHandler(ICurrentUserService currentUserService,
        UserManager<AppUser> userManager)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<Result> Handle(CreateUser request, CancellationToken cancellationToken)
    {
        var appUser = new AppUser(
            request.TenantId == 0 ? _currentUserService.TenantId : request.TenantId,
            request.FirstName,
            request.LastName,
            request.UserName,
            request.Email,
            request.Phone,
            _currentUserService.UserId,
            request.Roles
            );

        var result = await _userManager.CreateAsync(appUser, request.Password);

        return result.ToApplicationResult(appUser.Id);
    }
}
