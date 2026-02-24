namespace Admin.Core.Tenants.UseCases;

internal sealed class TenantCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TenantCommandUseCase> _logger;
    private readonly ICodeSequenceGenerator _codeGenerator;

    public TenantCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TenantCommandUseCase> logger,
        ICodeSequenceGenerator codeGenerator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _codeGenerator = codeGenerator;
    }

    public async Task<ApiResult<int>> CreateTenant(
        TenantDetail request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating tenant {TenantName} by user {UserId}",
            request.TenantName, _currentUserService.UserId);

        var tenantName = request.TenantName?.Trim() ?? string.Empty;

        var nameExists = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.TenantName.ToLower() == tenantName.ToLower(), cancellationToken);

        if (nameExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.name.duplicate", "Tenant name already exists."));
        }

        var authSettings = TenantAuthSetting.Create(0);

        authSettings.SetAuthenticationMode(request.AuthSettings.AuthenticationMode);

        if (request.AuthSettings.AllowLocalLogin) authSettings.EnableLocalLogin();
        else authSettings.DisableLocalLogin();

        if (request.AuthSettings.RequireEmailVerification) authSettings.RequireVerifiedEmail();
        else authSettings.AllowUnverifiedEmail();

        if (request.AuthSettings.AllowSelfRegistration) authSettings.EnableSelfRegistration();
        else authSettings.DisableSelfRegistration();

        if (request.AuthSettings.TwoFactorEnabled)
            authSettings.EnableTwoFactor(TimeSpan.FromMinutes(request.AuthSettings.TwoFactorCodeExpiry ?? 5));
        else
            authSettings.DisableTwoFactor();

        var tenantUISetting = TenantUISetting.Create(request.UISetting.Theme,
            request.UISetting.LogoUrl,
            request.UISetting.PrimaryColor,
            request.UISetting.DefaultLanguage,
            request.UISetting.LoginText);

        var createResult = Tenant.Create(
            tenantName: tenantName,
            email: request.Email?.Trim(),
            isActive: request.IsActive,
            authSetting: authSettings,
            tenantUISetting: tenantUISetting,
            out var tenant);

        if (!createResult.IsSuccess || tenant is null)
        {
            return FailureFromResult(createResult);
        }

        var nextValue = await _codeGenerator
            .NextTenantCodeAsync(_currentUserService.TenantId, cancellationToken);

        tenant.GenerateTenantCode(nextValue);

        foreach (var p in request.Providers)
        {
            var config = OidcClientConfig.Create(
                clientId: p.ClientId,
                authority: new Uri(p.Authority),
                scopes: p.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                callbackPath: p.CallbackPath,
                clientSecret: p.ClientSecret);

            var addResult = tenant.AddExternalProvider(p.ProviderType, config);
            if (!addResult.IsSuccess)
                return FailureFromResult(addResult);

            if (!p.Enabled)
                tenant.DisableExternalProvider(p.ProviderType);
        }

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Tenant created with Id {TenantId}", tenant.Id);

        return ApiResult<int>.Success(tenant.Id);
    }

    public async Task<ApiResult<int>> UpdateTenant(int id,
        TenantDetail request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating tenant {TenantId}", id);

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

        var updateResult = tenant.UpdateTenantProfile(
            tenantName,
            request.Email?.Trim(),
            request.IsActive);

        if (!updateResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                updateResult.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
        }

        tenant.TenantUISetting.Update(request.UISetting.Theme,
            request.UISetting.LogoUrl,
            request.UISetting.PrimaryColor,
            request.UISetting.DefaultLanguage,
            request.UISetting.LoginText);

        tenant.ConfigureAuthSettings(auth =>
        {
            auth.SetAuthenticationMode(request.AuthSettings.AuthenticationMode);

            if (request.AuthSettings.AllowLocalLogin) auth.EnableLocalLogin();
            else auth.DisableLocalLogin();

            if (request.AuthSettings.RequireEmailVerification) auth.RequireVerifiedEmail();
            else auth.AllowUnverifiedEmail();

            if (request.AuthSettings.AllowSelfRegistration) auth.EnableSelfRegistration();
            else auth.DisableSelfRegistration();

            if (request.AuthSettings.TwoFactorEnabled)
                auth.EnableTwoFactor(TimeSpan.FromMinutes(request.AuthSettings.TwoFactorCodeExpiry ?? 5));
            else
                auth.DisableTwoFactor();
        });

        foreach (var p in request.Providers)
        {
            var config = OidcClientConfig.Create(
                p.ClientId,
                new Uri(p.Authority),
                p.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                p.CallbackPath,
                p.ClientSecret);

            if (!tenant.TenantExternalProviders.Any(x => x.ProviderType == p.ProviderType))
            {
                tenant.AddExternalProvider(p.ProviderType, config);
            }
            else
            {
                tenant.UpdateExternalProviderConfig(p.ProviderType, config);
            }

            if (p.Enabled)
                tenant.EnableExternalProvider(p.ProviderType);
            else
                tenant.DisableExternalProvider(p.ProviderType);
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

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }
}