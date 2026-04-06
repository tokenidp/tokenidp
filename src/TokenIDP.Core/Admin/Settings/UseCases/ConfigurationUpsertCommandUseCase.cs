using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Settings.UseCases;

internal sealed class ConfigurationUpsertCommandUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICache _cache;
    private readonly IAppLogger<ConfigurationUpsertCommandUseCase> _logger;

    public ConfigurationUpsertCommandUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        ICache cache,
        IAppLogger<ConfigurationUpsertCommandUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResult<TenantConfigurationDto>> UpsertConfiguration(
        CreateUpdateConfiguration request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<TenantConfigurationDto>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ConfigKey))
        {
            return ApiResult<TenantConfigurationDto>.Failure(
                ApiError.Failure("configuration.key.invalid", "Configuration key is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ConfigValue))
        {
            return ApiResult<TenantConfigurationDto>.Failure(
                ApiError.Failure("configuration.value.invalid", "Configuration value is required."));
        }

        var normalizedKey = TenantConfigurationValidation.NormalizeKey(request.ConfigKey);
        var valueValidation = TenantConfigurationValidation.ValidateValue(request.ValueType, request.ConfigValue);
        if (!valueValidation.IsSuccess)
        {
            return ApiResult<TenantConfigurationDto>.Failure(valueValidation.Errors
                .Select(e => ApiError.Failure(e.Code, e.Message))
                .ToList());
        }

        var configuration = await _repository.GetByKeyAsync(tenantId, normalizedKey, cancellationToken);
        if (configuration == null)
        {
            var createResult = Configuration.Create(
                tenantId,
                normalizedKey,
                request.ConfigValue,
                request.ValueType,
                request.Scope,
                request.IsEditable,
                out var newConfiguration);

            if (!createResult.IsSuccess || newConfiguration == null)
            {
                return ApiResult<TenantConfigurationDto>.Failure(createResult.Errors
                    .Select(e => ApiError.Failure(e.Code, e.Message))
                    .ToList());
            }

            await _repository.AddAsync(newConfiguration, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var createdDto = TenantConfigurationDto.Projection.Compile().Invoke(newConfiguration);
            return ApiResult<TenantConfigurationDto>.Success(createdDto);
        }

        if (!configuration.IsEditable)
        {
            return ApiResult<TenantConfigurationDto>.Failure(ApiError.Failure("configuration.readonly",
                "Configuration is read-only."));
        }

        var updateResult = configuration.UpdateConfiguration(
            request.ConfigValue,
            request.ValueType,
            request.Scope,
            request.IsEditable);

        if (!updateResult.IsSuccess)
        {
            return ApiResult<TenantConfigurationDto>.Failure(updateResult.Errors
                .Select(e => ApiError.Failure(e.Code, e.Message))
                .ToList());
        }

        _repository.Update(configuration);
        await _repository.SaveChangesAsync(cancellationToken);

        var cacheKey = CacheKeys.CONFIGURATION.FormatCacheKey("Key", tenantId, normalizedKey);
        await _cache.RemoveAsync(cacheKey);

        var dto = TenantConfigurationDto.Projection.Compile().Invoke(configuration);
        _logger.LogInfo("Configuration upserted for tenant {TenantId} key {ConfigKey}",
            tenantId, normalizedKey);

        return ApiResult<TenantConfigurationDto>.Success(dto);
    }
}

