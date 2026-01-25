using Admin.Core.Common;
using Admin.Core.Configurations;

namespace Admin.Core.Settings.UseCases;

internal sealed class ConfigurationUpdateCommandUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICache _cache;
    private readonly IAppLogger<ConfigurationUpdateCommandUseCase> _logger;

    public ConfigurationUpdateCommandUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        ICache cache,
        IAppLogger<ConfigurationUpdateCommandUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResult<int>> UpdateConfiguration(
        int id,
        CreateUpdateConfiguration request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ConfigValue))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("configuration.value.invalid", "Configuration value is required."));
        }

        var valueValidation = TenantConfigurationValidation.ValidateValue(request.ValueType, request.ConfigValue);
        if (!valueValidation.IsSuccess)
        {
            return ApiResult<int>.Failure(valueValidation.Errors
                .Select(e => ApiError.Failure(e.Code, e.Message))
                .ToList());
        }

        var configuration = await _repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (configuration == null)
        {
            _logger.LogWarning("Configuration not found for update: {ConfigId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Configuration not found for the Id {0}".FormatString(id)));
        }

        if (!configuration.IsEditable)
        {
            return ApiResult<int>.Failure(ApiError.Failure("configuration.readonly",
                "Configuration is read-only."));
        }

        var updateResult = configuration.UpdateConfiguration(
            request.ConfigValue,
            request.ValueType,
            request.Scope,
            request.IsEditable);

        if (!updateResult.IsSuccess)
        {
            return ApiResult<int>.Failure(updateResult.Errors
                .Select(e => ApiError.Failure(e.Code, e.Message))
                .ToList());
        }

        _repository.Update(configuration);
        var result = await _repository.SaveChangesAsync(cancellationToken);

        var normalizedKey = TenantConfigurationValidation.NormalizeKey(configuration.ConfigKey);
        var cacheKey = CacheKeys.CONFIGURATION.FormatCacheKey("Key", tenantId, normalizedKey);
        await _cache.RemoveAsync(cacheKey);

        _logger.LogInfo("Configuration updated {ConfigId}", id);

        return ApiResult<int>.Success(result);
    }
}
