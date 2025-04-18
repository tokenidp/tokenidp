using Identity.Application.Identity;
using Identity.Application.Identity.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;

    public IdentityService(UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenGenerator tokenGenerator,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenGenerator = tokenGenerator;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<AuthResponse> Authenticate(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);

        if (user == null)
        {
            return new(false,
                $"User with {userName} not found.");
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password,
            false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return new(false,
                $"Credentials for '{userName} aren't valid'.");
        }

        return await GenerateResponse(user, userName);
    }

    public Task<AuthResponse> RefreshToken(string refreshToken)
    {
        throw new System.NotImplementedException();
    }

    private async Task<AuthResponse> GenerateResponse(AppUser user, string userName)
    {
        AuthResponse response = default;

        var claims = await _dbContext.UsersClaims
            .Where(c => c.UserId == user.Id)
            .ProjectTo<ClaimDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        if (!claims.IsSafe())
        {
            return new(false,
                $"Claims not found for '{userName}'.");
        }

        var roles = claims.Where(s => !string.IsNullOrEmpty(s.RoleName))
            .Select(s => s.RoleName).Distinct().ToArray();

        var token = _tokenGenerator.GetAccessToken(
            user.Id.ToString(),
            user.UserName,
            user.TenantId.ToString(),
            roles);

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);

        response = new(true, string.Empty);

        var defaultReport = claims
            .Where(s => s.IsDefaultReport).Select(s => s.ReportId).FirstOrDefault();

        response.SetResponse(token,
            user.Id,
            user.TenantId,
            user.FullName,
            tenant.LandingPage,
            tenant.Theme,
            defaultReport,
            tenant.IsParentTenant,
            claims);

        return response;
    }
}
