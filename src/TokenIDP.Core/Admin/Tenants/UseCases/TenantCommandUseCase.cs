using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Foundation;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Tenants.UseCases;

internal sealed class TenantCommandUseCase
{
    private const int SystemTenantId = 1;
    private readonly ITenantRepository _tenantRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TenantCommandUseCase> _logger;
    private readonly ICodeSequenceGenerator _codeGenerator;
    private readonly ISecretProtector _secretProtector;

    public TenantCommandUseCase(
        ITenantRepository tenantRepository,
        IClientRepository clientRepository,
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<TenantCommandUseCase> logger,
        ICodeSequenceGenerator codeGenerator,
        ISecretProtector secretProtector)
    {
        _tenantRepository = tenantRepository;
        _clientRepository = clientRepository;
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
        _codeGenerator = codeGenerator;
        _secretProtector = secretProtector;
    }

    public async Task<ApiResult<int>> CreateTenant(
        CreateUpdateTenant request,
        CancellationToken cancellationToken = default)
    {
        var authSettingsRequest = request.AuthSettings ?? new TenantAuthSettingDetail();
        var uiSettingsRequest = request.UISetting ?? new TenantUISettingDetail();

        _logger.LogDebug("Creating tenant {TenantName} by user {UserId}",
            request.TenantName, _currentUserService.UserId);

        var tenantName = request.TenantName?.Trim() ?? string.Empty;

        var nameExists = await _tenantRepository.TenantNameExistsAsync(
            tenantName,
            null,
            cancellationToken);

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

        await _tenantRepository.AddAsync(tenant, cancellationToken);
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

        _logger.LogDebug("Updating tenant {TenantId}", id);

        if (IsCrossTenantAccessDenied(id))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _tenantRepository.GetTenantAggregateAsync(id, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for update: {TenantId}", id);

            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(id)));
        }

        var tenantName = request.TenantName?.Trim() ?? string.Empty;

        var nameExists = await _tenantRepository.TenantNameExistsAsync(
            tenantName,
            id,
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

        await _tenantRepository.SaveChangesAsync(cancellationToken);
        await InvalidateLookupCaches(id);

        _logger.LogInfo("Tenant updated {TenantId}", id);

        return ApiResult<int>.Success(tenant.Id);
    }

    public async Task<ApiResult<TenantSocialProviderDetail>> UpdateTenantSocialProvider(
        int tenantId,
        ExternalProviderTypes providerType,
        UpdateTenantSocialProvider request,
        CancellationToken cancellationToken = default)
    {
        if (IsCrossTenantAccessDenied(tenantId))
        {
            return ApiResult<TenantSocialProviderDetail>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _tenantRepository.GetTenantAggregateAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ApiResult<TenantSocialProviderDetail>.Failure(
                ApiError.Failure("NotFound", "Tenant not found for the Id {0}".FormatString(tenantId)));
        }

        var existingProvider = tenant.TenantExternalProviders
            .FirstOrDefault(provider => provider.ProviderType == providerType);

        var hasAnySubmittedConfig =
            !string.IsNullOrWhiteSpace(request.ClientId)
            || !string.IsNullOrWhiteSpace(request.ClientSecret)
            || !string.IsNullOrWhiteSpace(request.Scopes)
            || request.Enabled;

        if (existingProvider is null && !hasAnySubmittedConfig)
        {
            return ApiResult<TenantSocialProviderDetail>.Success(new TenantSocialProviderDetail
            {
                ProviderType = providerType,
                Enabled = false,
                HasClientSecret = false,
                ClientId = string.Empty,
                ClientSecret = null,
                Scopes = string.Empty
            });
        }

        var effectiveClientId = string.IsNullOrWhiteSpace(request.ClientId)
            ? existingProvider?.OidcConfig?.ClientId ?? string.Empty
            : request.ClientId.Trim();
        var effectiveScopes = string.IsNullOrWhiteSpace(request.Scopes)
            ? existingProvider?.OidcConfig?.Scopes ?? string.Empty
            : request.Scopes.Trim();
        var effectiveSecret = string.IsNullOrWhiteSpace(request.ClientSecret)
            ? existingProvider?.OidcConfig?.ClientSecret
            : EncryptProviderSecret(tenant.Id.ToString(), providerType, request.ClientSecret.Trim());

        if (request.Enabled)
        {
            if (string.IsNullOrWhiteSpace(effectiveClientId))
            {
                return ApiResult<TenantSocialProviderDetail>.Failure(
                    ApiError.Failure("tenant.provider.client_id.required",
                        "Client ID is required when provider is enabled."));
            }

            if (string.IsNullOrWhiteSpace(effectiveScopes))
            {
                return ApiResult<TenantSocialProviderDetail>.Failure(
                    ApiError.Failure("tenant.provider.scopes.required",
                        "Scopes are required when provider is enabled."));
            }

            if (string.IsNullOrWhiteSpace(effectiveSecret))
            {
                return ApiResult<TenantSocialProviderDetail>.Failure(
                    ApiError.Failure("tenant.provider.client_secret.required",
                        "Client secret is required when provider is enabled."));
            }
        }

        if (string.IsNullOrWhiteSpace(effectiveClientId))
        {
            return ApiResult<TenantSocialProviderDetail>.Failure(
                ApiError.Failure("tenant.provider.client_id.required",
                    "Client ID is required to configure the provider."));
        }

        var config = OidcClientConfig.Create(
            effectiveClientId,
            effectiveSecret,
            effectiveScopes);

        if (existingProvider is null)
        {
            var addResult = tenant.AddExternalProvider(providerType, config);
            if (!addResult.IsSuccess)
            {
                return FailureFromResult<TenantSocialProviderDetail>(addResult);
            }
        }
        else
        {
            var updateProviderResult = tenant.UpdateExternalProviderConfig(providerType, config);
            if (!updateProviderResult.IsSuccess)
            {
                return FailureFromResult<TenantSocialProviderDetail>(updateProviderResult);
            }
        }

        var toggleResult = request.Enabled
            ? tenant.EnableExternalProvider(providerType)
            : tenant.DisableExternalProvider(providerType);
        if (!toggleResult.IsSuccess)
        {
            return FailureFromResult<TenantSocialProviderDetail>(toggleResult);
        }

        await _tenantRepository.SaveChangesAsync(cancellationToken);
        await InvalidateLookupCaches(tenantId);
        await InvalidateProviderCaches(tenantId, providerType, cancellationToken);

        return ApiResult<TenantSocialProviderDetail>.Success(new TenantSocialProviderDetail
        {
            ProviderType = providerType,
            Enabled = request.Enabled,
            HasClientSecret = !string.IsNullOrWhiteSpace(effectiveSecret),
            ClientId = effectiveClientId,
            ClientSecret = !string.IsNullOrWhiteSpace(effectiveSecret) ? "********" : null,
            Scopes = effectiveScopes
        });
    }

    public async Task<ApiResult<int>> DeleteTenant(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting tenant {TenantId}", tenantId);

        if (IsCrossTenantAccessDenied(tenantId))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _tenantRepository.GetTenantAggregateAsync(tenantId, cancellationToken);

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

        var deleteResult = tenant.SoftDelete();
        if (!deleteResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                deleteResult.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
        }

        await _tenantRepository.SaveChangesAsync(cancellationToken);
        await InvalidateLookupCaches(tenantId);

        _logger.LogInfo("Tenant deleted {TenantId}", tenantId);

        return ApiResult<int>.Success(tenant.Id);
    }

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }

    private static ApiResult<T> FailureFromResult<T>(Result result)
    {
        return ApiResult<T>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }

    private async Task InvalidateLookupCaches(int tenantId)
    {
        await _cache.RemoveAsync($"{CacheKeys.LOOKUP}:client:{tenantId}");
        await _cache.RemoveAsync($"{CacheKeys.LOOKUP}:client:{tenantId}");
    }

    private async Task InvalidateProviderCaches(
        int tenantId,
        ExternalProviderTypes providerType,
        CancellationToken cancellationToken)
    {
        var clientIds = await _clientRepository.GetTenantClientIdsAsync(tenantId, cancellationToken);
        foreach (var clientId in clientIds)
        {
            await _cache.RemoveAsync("CLT".FormatCacheKey("EPRV", clientId));
            await _cache.RemoveAsync("CLT".FormatCacheKey("EPRV", tenantId, clientId, providerType));
        }
    }

    private async Task<bool> CheckTenantByKey(string key, CancellationToken cancellationToken)
    {
        return await _tenantRepository.TenantKeyExistsAsync(key, cancellationToken);
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

    private string? EncryptProviderSecret(
        string tenantId,
        ExternalProviderTypes providerType,
        string? clientSecret)
    {
        return _secretProtector.Encrypt(clientSecret, BuildSecretContext(tenantId, providerType));
    }

    private static string BuildSecretContext(string tenantId, ExternalProviderTypes providerType)
    {
        return $"tenant:{tenantId}:provider:{providerType}";
    }

    private bool IsCrossTenantAccessDenied(int tenantId)
    {
        return !HasGlobalTenantAccess()
               && _currentUserService.TenantId > 0
               && tenantId != _currentUserService.TenantId;
    }

    private bool HasGlobalTenantAccess()
    {
        return _currentUserService.TenantId <= 0 || _currentUserService.TenantId == SystemTenantId;
    }
}
