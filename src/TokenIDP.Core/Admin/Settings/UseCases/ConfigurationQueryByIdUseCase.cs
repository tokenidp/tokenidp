using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Admin.Configurations;

namespace TokenIDP.Core.Admin.Settings.UseCases;

internal sealed class ConfigurationQueryByIdUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ConfigurationQueryByIdUseCase> _logger;

    public ConfigurationQueryByIdUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        IAppLogger<ConfigurationQueryByIdUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<TenantConfigurationDto>> GetConfigurationById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<TenantConfigurationDto>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        var configuration = await _repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (configuration == null)
        {
            _logger.LogWarning("Configuration not found for tenant {TenantId} id {ConfigId}",
                tenantId, id);
            return ApiResult<TenantConfigurationDto>.Failure(ApiError.Failure("NotFound",
                "Configuration not found for the Id {0}".FormatString(id)));
        }

        var dto = TenantConfigurationDto.Projection.Compile().Invoke(configuration);
        return ApiResult<TenantConfigurationDto>.Success(dto);
    }
}

