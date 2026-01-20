using Admin.Core.Common;

namespace Admin.Core.Tenants;

internal sealed class CreateUpdateTenantUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<CreateUpdateTenantUseCase> _logger;

    public CreateUpdateTenantUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<CreateUpdateTenantUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<int>> CreateTenant(
        CreateUpdateTenant request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating tenant {TenantName} by user {UserId}",
            request.TenantName, _currentUserService.UserId);

        var tenantName = request.TenantName?.Trim() ?? string.Empty;
        var tenantCode = await ResolveTenantCode(request.TenantCode, tenantName, cancellationToken);

        var nameExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.TenantName.ToLower() == tenantName.ToLower(), cancellationToken);

        if (nameExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.name.duplicate", "Tenant name already exists."));
        }

        var codeExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.TenantCode.ToLower() == tenantCode.ToLower(), cancellationToken);

        if (codeExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.code.duplicate", "Tenant code already exists."));
        }

        var createResult = Tenant.Create(
            tenantName,
            tenantCode,
            request.Email?.Trim(),
            request.Theme?.Trim(),
            request.LogoUrl?.Trim(),
            request.PrimaryColor?.Trim(),
            request.DefaultLanguage?.Trim(),
            request.LoginText?.Trim(),
            request.TwoFactorEnabled,
            request.TwoFactorCodeExpiry,
            request.HomePageUrl?.Trim(),
            request.IsActive,
            request.TenantType,
            request.SubscriptionType,
            request.AuthenticationMode,
            out var tenant);

        if (!createResult.IsSuccess || tenant == null)
        {
            return FailureFromResult(createResult);
        }

        //var roles = await _dbContext.Roles.Where(s => s.IsEditable).ToListAsync();

        //var claims = await _dbContext.Permissions.ToListAsync();

        //var configurations = await _dbContext.Configurations.Where(s => s.IsEditable).ToListAsync();

        //foreach (var role in roles)
        //{
        //    tenant.AddTenantRoles(role.Name, role.RoleDescription);
        //}

        //foreach (var claim in claims)
        //{
        //    //tenant.AddPermission(claim.Id);
        //}

        //foreach (var configuration in configurations)
        //{
        //    tenant.AddTenantConfigurations(configuration.ConfigKey,
        //        configuration.ConfigValue,
        //        configuration.IsEditable);
        //}

        _dbContext.Tenants.Add(tenant);

        await _dbContext.SaveChangesAsync(cancellationToken);

        //await AddRolePermissions(tenant);

        _logger.LogInfo("Tenant created with Id {TenantId}", tenant.Id);

        return ApiResult<int>.Success(tenant.Id);
    }

    public async Task<ApiResult<int>> UpdateTenant(
        int id,
        CreateUpdateTenant request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating tenant {TenantId}", id);

        if (_currentUserService.TenantId > 0 && id != _currentUserService.TenantId)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for update: {TenantId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(id)));
        }

        var tenantName = request.TenantName?.Trim() ?? string.Empty;
        var nameExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id != id &&
                t.TenantName.ToLower() == tenantName.ToLower(),
                cancellationToken);

        if (nameExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.name.duplicate", "Tenant name already exists."));
        }

        if (!string.IsNullOrWhiteSpace(request.TenantCode) &&
            !string.Equals(tenant.TenantCode, request.TenantCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.code.immutable", "Tenant code cannot be changed."));
        }

        var updateResult = tenant.UpdateTenant(
            tenantName,
            request.Email?.Trim(),
            request.Theme?.Trim(),
            request.LogoUrl?.Trim(),
            request.PrimaryColor?.Trim(),
            request.DefaultLanguage?.Trim(),
            request.LoginText?.Trim(),
            request.TwoFactorEnabled,
            request.TwoFactorCodeExpiry,
            request.HomePageUrl?.Trim(),
            request.IsActive,
            request.TenantType,
            request.SubscriptionType,
            request.AuthenticationMode);

        if (!updateResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                updateResult.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Tenant updated {TenantId}", id);

        return ApiResult<int>.Success(tenant.Id);
    }

    public async Task<ApiResult<int>> DeleteTenant(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting tenant {TenantId}", tenantId);

        if (_currentUserService.TenantId > 0 && tenantId != _currentUserService.TenantId)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for delete: {TenantId}", tenantId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(tenantId)));
        }

        if (tenant.IsActive == true)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.delete.active", "Active tenants cannot be deleted."));
        }

        var deleteResult = tenant.Disable();
        if (!deleteResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                deleteResult.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Tenant deleted {TenantId}", tenantId);

        return ApiResult<int>.Success(tenant.Id);
    }

    private async Task<string> ResolveTenantCode(
        string? tenantCode,
        string tenantName,
        CancellationToken cancellationToken)
    {
        var normalized = tenantCode?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized.ToUpperInvariant();
        }

        var baseCode = new string(tenantName.Where(char.IsLetterOrDigit).ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(baseCode))
        {
            baseCode = "TENANT";
        }

        baseCode = baseCode.Length >= 4 ? baseCode[..4] : baseCode.PadRight(4, 'X');
        var candidate = baseCode;
        var suffix = 1;

        while (await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.TenantCode.ToLower() == candidate.ToLower(), cancellationToken))
        {
            candidate = $"{baseCode}{suffix:00}";
            suffix++;
        }

        return candidate;
    }

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
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