namespace Admin.Core.Tenants;

internal class TenantUseCases
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<TenantUseCases> _logger;

    public TenantUseCases(IApplicationDbContext dbContext, ICache cache, IAppLogger<TenantUseCases> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResult<int>> CreateTenant(CreateUpdateTenant request)
    {
        _logger.LogDebug("Creating tenant {TenantName}", request.TenantName);

        Tenant tenant = new(request.TenantName,
              "001",
              request.Email,
              request.Theme,
              request.Logo,
              request.LandingPage,
              request.IsActive);

        var roles = await _dbContext.Roles.Where(s => s.IsEditable).ToListAsync();

        var claims = await _dbContext.Permissions.ToListAsync();

        var configurations = await _dbContext.Configurations.Where(s => s.IsEditable).ToListAsync();

        foreach (var role in roles)
        {
            tenant.AddTenantRoles(role.Name, role.RoleDescription);
        }

        foreach (var claim in claims)
        {
            //tenant.AddPermission(claim.Id);
        }

        foreach (var configuration in configurations)
        {
            tenant.AddTenantConfigurations(configuration.ConfigKey,
                configuration.ConfigValue,
                configuration.IsEditable);
        }

        _dbContext.Tenants.Add(tenant);

        var result = await _dbContext.SaveChangesAsync();

        await AddRolePermissions(tenant);

        _logger.LogInfo("Tenant created with Id {TenantId}", tenant.Id);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<int>> UpdateTenant(int id, CreateUpdateTenant request)
    {
        _logger.LogDebug("Updating tenant {TenantId}", id);

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for update: {TenantId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(id)));
        }

        tenant.UpdateTenant(request.TenantName,
              request.Email,
              request.Theme,
              request.Logo,
              request.LandingPage,
              request.IsActive);

        _dbContext.Tenants.Update(tenant);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Tenant updated {TenantId}", id);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<TenantDto>> GetTenantById(int tenantId)
    {
        _logger.LogDebug("Fetching tenant {TenantId}", tenantId);

        var tenant = await _dbContext.Tenants
            .Where(u => u.Id == tenantId)
            .Select(TenantDto.Projection)
            .FirstOrDefaultAsync();

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found: {TenantId}", tenantId);
            return ApiResult<TenantDto>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(tenantId)));
        }

        return ApiResult<TenantDto>.Success(tenant);
    }

    public async Task<ApiResult<PaginatedList<TenantSearchDto>>> GetTenants(SearchData request)
    {
        _logger.LogDebug("Fetching tenants list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var users = await _dbContext.TenantsSearch
           .AsNoTracking()
           .Select(TenantSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} tenants", users.TotalCount);

        return ApiResult<PaginatedList<TenantSearchDto>>.Success(users);
    }

    public async Task<ApiResult<bool>> CheckTwoFactorEnabled(int tenantId)
    {
        _logger.LogDebug("Checking two-factor status for tenant {TenantId}", tenantId);

        var cacheKey = CacheKeys.TENANT.FormatCacheKey("TwoFactor", tenantId);

        var hasTwoFactorEnabled = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _dbContext.Tenants.Where(t => t.Id == tenantId)
            .Select(s => s.TwoFactorEnabled)
            .FirstOrDefaultAsync();

        }, new TimeSpan(0, 15, 0));

        _logger.LogDebug("Two-factor status resolved for tenant {TenantId}", tenantId);

        return ApiResult<bool>.Success(hasTwoFactorEnabled ?? false);
    }

    private async Task AddRolePermissions(Tenant tenant)
    {
        _logger.LogDebug("Adding role permissions for tenant {TenantId}", tenant.Id);

        List<RolePermission> roleClaims = new();

        //foreach (var role in tenant.Roles)
        //{
        //    roleClaims = (from ct in tenant.TenantPermissions
        //                  select new RolePermission
        //                  (
        //                     role.Id,
        //                      ct.Id

        //                  )).ToList();
        //}

        _dbContext.RolePermissions.AddRange(roleClaims);

        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Role permissions added for tenant {TenantId}", tenant.Id);
    }
}
