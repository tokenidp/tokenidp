using Admin.Core.Common;
using IDP.Foundation;

namespace Admin.Core.Tenants.UseCases;

internal sealed class TenantCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TenantCommandUseCase> _logger;
    private readonly ICodeSequenceGenerator _codeGenerator;

    public TenantCommandUseCase(
        IApplicationDbContext dbContext,
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<TenantCommandUseCase> logger,
        ICodeSequenceGenerator codeGenerator)
    {
        _dbContext = dbContext;
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
        _codeGenerator = codeGenerator;
    }

    public async Task<ApiResult<int>> CreateTenant(
        CreateUpdateTenant request,
        CancellationToken cancellationToken = default)
    {
        var authSettingsRequest = request.AuthSettings ?? new TenantAuthSettingDetail();
        var uiSettingsRequest = request.UISetting ?? new TenantUISettingDetail();
        var providersRequest = request.Providers ?? new List<TenantExternalProviderDetail>();

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

        var tenantKey = await GenerateUniqueAsync(tenantName,
            key => CheckTenantByKey(key, cancellationToken));

        if (string.IsNullOrEmpty(tenantKey))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.key.duplicate", "Tenant key already exists."));
        }

        var authSettings = TenantAuthSetting.Create(0);

        authSettings.SetAuthenticationMode(authSettingsRequest.AuthenticationMode);

        if (authSettingsRequest.AllowLocalLogin) authSettings.EnableLocalLogin();
        else authSettings.DisableLocalLogin();

        if (authSettingsRequest.RequireEmailVerification) authSettings.RequireVerifiedEmail();
        else authSettings.AllowUnverifiedEmail();

        if (authSettingsRequest.AllowSelfRegistration) authSettings.EnableSelfRegistration();
        else authSettings.DisableSelfRegistration();

        if (authSettingsRequest.TwoFactorEnabled)
            authSettings.EnableTwoFactor(TimeSpan.FromMinutes(authSettingsRequest.TwoFactorCodeExpiry ?? 5));
        else
            authSettings.DisableTwoFactor();

        var tenantUISetting = TenantUISetting.Create(uiSettingsRequest.Theme,
            uiSettingsRequest.LogoUrl,
            uiSettingsRequest.PrimaryColor,
            uiSettingsRequest.DefaultLanguage,
            uiSettingsRequest.LoginText);

        var createResult = Tenant.Create(
            tenantName: tenantName,
            tenantKey: tenantKey,
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

        foreach (var p in providersRequest)
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
        await InvalidateLookupCaches(tenant.Id);

        _logger.LogInfo("Tenant created with Id {TenantId}", tenant.Id);

        return ApiResult<int>.Success(tenant.Id);
    }

    public async Task<ApiResult<int>> UpdateTenant(int id,
        CreateUpdateTenant request,
        CancellationToken cancellationToken = default)
    {
        var authSettingsRequest = request.AuthSettings ?? new TenantAuthSettingDetail();
        var uiSettingsRequest = request.UISetting ?? new TenantUISettingDetail();
        var providersRequest = request.Providers ?? new List<TenantExternalProviderDetail>();

        _logger.LogDebug("Updating tenant {TenantId}", id);

        var tenant = await _dbContext.Tenants
            .Include(t => t.TenantUISetting)
            .Include(t => t.TenantAuthSetting)
            .Include(t => t.TenantExternalProviders)
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

        if (tenant.TenantUISetting is null)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.ui.missing", "Tenant UI settings are missing."));
        }

        tenant.TenantUISetting.Update(uiSettingsRequest.Theme,
            uiSettingsRequest.LogoUrl,
            uiSettingsRequest.PrimaryColor,
            uiSettingsRequest.DefaultLanguage,
            uiSettingsRequest.LoginText);

        var authConfigureResult = tenant.ConfigureAuthSettings(auth =>
        {
            auth.SetAuthenticationMode(authSettingsRequest.AuthenticationMode);

            if (authSettingsRequest.AllowLocalLogin) auth.EnableLocalLogin();
            else auth.DisableLocalLogin();

            if (authSettingsRequest.RequireEmailVerification) auth.RequireVerifiedEmail();
            else auth.AllowUnverifiedEmail();

            if (authSettingsRequest.AllowSelfRegistration) auth.EnableSelfRegistration();
            else auth.DisableSelfRegistration();

            if (authSettingsRequest.TwoFactorEnabled)
                auth.EnableTwoFactor(TimeSpan.FromMinutes(authSettingsRequest.TwoFactorCodeExpiry ?? 5));
            else
                auth.DisableTwoFactor();
        });

        if (!authConfigureResult.IsSuccess)
        {
            return FailureFromResult(authConfigureResult);
        }

        foreach (var p in providersRequest)
        {
            var existingProvider = tenant.TenantExternalProviders
                .FirstOrDefault(x => x.ProviderType == p.ProviderType);

            var resolvedClientSecret = string.IsNullOrWhiteSpace(p.ClientSecret)
                ? existingProvider?.OidcConfig?.ClientSecret
                : p.ClientSecret;

            var config = OidcClientConfig.Create(
                p.ClientId,
                new Uri(p.Authority),
                p.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                p.CallbackPath,
                resolvedClientSecret);

            if (existingProvider is null)
            {
                var addResult = tenant.AddExternalProvider(p.ProviderType, config);
                if (!addResult.IsSuccess)
                    return FailureFromResult(addResult);
            }
            else
            {
                var updateProviderResult = tenant.UpdateExternalProviderConfig(p.ProviderType, config);
                if (!updateProviderResult.IsSuccess)
                    return FailureFromResult(updateProviderResult);
            }

            if (p.Enabled)
            {
                var enableResult = tenant.EnableExternalProvider(p.ProviderType);
                if (!enableResult.IsSuccess)
                    return FailureFromResult(enableResult);
            }
            else
            {
                var disableResult = tenant.DisableExternalProvider(p.ProviderType);
                if (!disableResult.IsSuccess)
                    return FailureFromResult(disableResult);
            }
        }

        // Any existing provider missing from the request is treated as unchecked.
        var requestedProviderTypes = providersRequest
            .Select(p => p.ProviderType)
            .ToHashSet();

        foreach (var existingProviderType in tenant.TenantExternalProviders
                     .Select(p => p.ProviderType)
                     .Where(type => !requestedProviderTypes.Contains(type)))
        {
            var disableMissingResult = tenant.DisableExternalProvider(existingProviderType);
            if (!disableMissingResult.IsSuccess)
                return FailureFromResult(disableMissingResult);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateLookupCaches(id);

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

    private async Task InvalidateLookupCaches(int tenantId)
    {
        await _cache.RemoveAsync($"{CacheKeys.LOOKUP}:client:{tenantId}");
        await _cache.RemoveAsync($"{CacheKeys.LOOKUP}:client:{tenantId}");
    }

    private async Task<bool> CheckTenantByKey(string key, CancellationToken cancellationToken)
    {
        var keyExist = await _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.TenantKey.ToLower() == key.ToLower(), cancellationToken);

        return keyExist;
    }

    private static async Task<string> GenerateUniqueAsync(string tenantName,
        Func<string, Task<bool>> existsAsync)
    {
        var baseKey = TenantKeyGenerator.Generate(tenantName);

        var key = baseKey;
        var counter = 1;

        while (await existsAsync(key))
        {
            key = $"{baseKey}-{counter}";
            counter++;
        }

        return key;
    }
}