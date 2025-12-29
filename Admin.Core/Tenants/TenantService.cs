using Admin.Core;

namespace Admin.Core.Tenants;

internal class TenantService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;

    public TenantService(ApplicationDbContext dbContext, ICache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result> CreateTenant(CreateUpdateTenant request)
    {
        Tenant tenant = new(request.TenantName,
              "001",
              request.Email,
              request.Theme,
              request.Logo,
              request.LandingPage,
              request.IsActive);

        var roles = await _dbContext.Roles.Where(s => s.IsEditable).ToListAsync();

        var claims = await _dbContext.Permissions.Where(s => s.IsEditable).ToListAsync();

        var configurations = await _dbContext.Configurations.Where(s => s.IsEditable).ToListAsync();

        foreach (var role in roles)
        {
            tenant.AddTenantRoles(role.Name, role.RoleDescription);
        }

        foreach (var claim in claims)
        {
            tenant.AddTenantClaims(claim.Id, claim.PermissionType);
        }

        foreach (var configuration in configurations)
        {
            tenant.AddTenantConfigurations(configuration.ConfigKey,
                configuration.ConfigValue,
                configuration.IsDisplay,
                configuration.IsEditable);
        }

        _dbContext.Tenants.Add(tenant);

        var result = await _dbContext.SaveChangesAsync();

        await AddRolePermissions(tenant);

        return Result.Success(result);
    }

    public async Task<Result> UpdateTenant(int id, CreateUpdateTenant request)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            return Result.Failure("NotFound", "Tenant not found for the Id {0}".FormatString(id));
        }

        tenant.UpdateTenant(request.TenantName,
              request.Email,
              request.Theme,
              request.Logo,
              request.LandingPage,
              request.IsActive);

        _dbContext.Tenants.Update(tenant);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }

    public async Task<TenantDto?> GetTenantById(int tenantId)
    {
        var tenant = await _dbContext.Tenants
            .Where(u => u.Id == tenantId)
            .Select(TenantDto.Projection)
            .FirstOrDefaultAsync();

        return tenant;
    }

    public async Task<PaginatedList<TenantSearchDto>> GetTenants(SearchData request)
    {
        var users = await _dbContext.TenantsSearch
           .AsNoTracking()
           .Select(TenantSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return users;
    }

    public async Task<bool> CheckTwoFactorEnabled(int tenantId)
    {
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("TwoFactor", tenantId);

        var hasTwoFactorEnabled = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _dbContext.Tenants.Where(t => t.Id == tenantId)
            .Select(s => s.TwoFactorEnabled)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        return hasTwoFactorEnabled;
    }

    private async Task AddRolePermissions(Tenant tenant)
    {
        List<RolePermission> roleClaims = new();

        foreach (var role in tenant.Roles)
        {
            roleClaims = (from ct in tenant.TenantPermissions
                          select new RolePermission
                          (
                              ct.Id,
                              role.Id,
                              ct.ClaimType,
                              "true"
                          )).ToList();
        }

        _dbContext.RolePermissions.AddRange(roleClaims);

        await _dbContext.SaveChangesAsync();
    }
}
