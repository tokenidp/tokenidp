using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Configurations;

namespace TokenIDP.Core.Admin.Settings.UseCases;

internal sealed class ConfigurationQueryByKeyUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICache _cache;
    private readonly IAppLogger<ConfigurationQueryByKeyUseCase> _logger;

    public ConfigurationQueryByKeyUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        ICache cache,
        IAppLogger<ConfigurationQueryByKeyUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResult<TenantConfigurationDto>> GetConfigurationByKey(
        string key,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<TenantConfigurationDto>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return ApiResult<TenantConfigurationDto>.Failure(
                ApiError.Failure("configuration.key.invalid", "Configuration key is required."));
        }

        var normalizedKey = TenantConfigurationValidation.NormalizeKey(key);
        var cacheKey = CacheKeys.CONFIGURATION.FormatCacheKey("Key", tenantId, normalizedKey);

        var configuration = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            return await _repository.GetByKeyAsync(tenantId, normalizedKey, cancellationToken);
        }, new TimeSpan(0, 5, 0));

        if (configuration == null)
        {
            _logger.LogWarning("Configuration not found for tenant {TenantId} key {ConfigKey}",
                tenantId, normalizedKey);
            return ApiResult<TenantConfigurationDto>.Failure(ApiError.Failure("NotFound",
                "Configuration not found for the key {0}".FormatString(normalizedKey)));
        }

        var dto = TenantConfigurationDto.Projection.Compile().Invoke(configuration);
        return ApiResult<TenantConfigurationDto>.Success(dto);
    }
}

