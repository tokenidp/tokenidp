using Admin.Core.Common;

namespace Admin.Core.Configurations;

internal sealed class DeleteTenantConfigurationUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICache _cache;
    private readonly IAppLogger<DeleteTenantConfigurationUseCase> _logger;

    public DeleteTenantConfigurationUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        ICache cache,
        IAppLogger<DeleteTenantConfigurationUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResult<int>> DeleteConfiguration(
        int id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        var configuration = await _repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (configuration == null)
        {
            _logger.LogWarning("Configuration not found for delete: {ConfigId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Configuration not found for the Id {0}".FormatString(id)));
        }

        if (!configuration.IsEditable)
        {
            return ApiResult<int>.Failure(ApiError.Failure("configuration.readonly",
                "Configuration is read-only."));
        }

        configuration.SoftDelete();
        _repository.Update(configuration);

        var result = await _repository.SaveChangesAsync(cancellationToken);

        var normalizedKey = TenantConfigurationValidation.NormalizeKey(configuration.ConfigKey);
        var cacheKey = CacheKeys.CONFIGURATION.FormatCacheKey("Key", tenantId, normalizedKey);
        await _cache.RemoveAsync(cacheKey);

        _logger.LogInfo("Configuration deleted {ConfigId}", id);

        return ApiResult<int>.Success(result);
    }
}
