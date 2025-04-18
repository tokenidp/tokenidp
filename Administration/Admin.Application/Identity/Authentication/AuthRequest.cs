namespace Identity.Application.Identity.Authentication;

public class AuthRequest : IRequest<AuthResponse>
{
    public string UserName { get; set; }
    public string Password { get; set; }
}

public class AuthRequestHandler : IRequestHandler<AuthRequest, AuthResponse>
{
    private readonly IIdentityService _identityService;

    public AuthRequestHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResponse> Handle(AuthRequest request, CancellationToken cancellationToken)
    {
        return await _identityService.Authenticate(request.UserName, request.Password);
    }
}
