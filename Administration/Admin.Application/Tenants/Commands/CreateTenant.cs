namespace Identity.Application.Tenants.Commands;

public class CreateTenant : IRequest<Result>
{
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public string Theme { get; set; }
    public string Logo { get; set; }
    public string TenantAppId { get; set; }
    public string LandingPage { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenant, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTenantCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(CreateTenant request, CancellationToken cancellationToken)
    {
        Tenant tenant = new(request.TenantName,
              request.TenantCode,
              request.Email,
              request.Theme,
              request.Logo,
              request.TenantAppId,
              request.LandingPage,
              request.IsActive);

        var roles = await _dbContext.AppRoles.Where(s => s.ShowToTenant).ToListAsync();

        var claims = await _dbContext.AppClaims.Where(s => s.ShowToTenant).ToListAsync();

        var configurations = await _dbContext.AppConfigurations.Where(s => s.ShowToTenant).ToListAsync();

        foreach (var role in roles)
        {
            tenant.AddTenantRoles(role.Name, role.RoleDescription);
        }

        foreach (var claim in claims)
        {
            tenant.AddTenantClaims(claim.Id, claim.ClaimType);
        }

        foreach (var configuration in configurations)
        {
            tenant.AddTenantConfigurations(configuration.ConfigKey,
                configuration.ConfigValue,
                configuration.IsDisplay,
                configuration.ShowToTenant);
        }

        _dbContext.Tenants.Add(tenant);

        var result = await _dbContext.SaveChangesAsync();

        await AddRoleClaims(tenant);

        return Result.Success(result);
    }

    private async Task AddRoleClaims(Tenant tenant)
    {
        List<AppRoleClaim> roleClaims = default;

        foreach (var role in tenant.AppRoles)
        {
            roleClaims = (from ct in tenant.AppClaimTenants
                          select new AppRoleClaim
                          (
                              ct.Id,
                              role.Id,
                              ct.ClaimType,
                              "true"
                          )).ToList();
        }

        _dbContext.AppRoleClaims.AddRange(roleClaims);

        await _dbContext.SaveChangesAsync();
    }
}
