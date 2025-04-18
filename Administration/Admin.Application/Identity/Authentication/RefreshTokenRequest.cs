namespace Identity.Application.Identity.Authentication;

public class RefreshTokenRequest : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; }
    public string IPAddress { get; set; }
}

[SuppressMessage("SonarLint", "S4487", Justification = "_identityService will use in future")]
public class RefreshTokenRequestHandler
    : IRequestHandler<RefreshTokenRequest, AuthResponse>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenRequestHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        return await Task.FromResult(new AuthResponse(true, string.Empty));
    }
}
