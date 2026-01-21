namespace Admin.Core.Configurations;

internal sealed class GetTenantConfigurationByIdUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<GetTenantConfigurationByIdUseCase> _logger;

    public GetTenantConfigurationByIdUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        IAppLogger<GetTenantConfigurationByIdUseCase> logger)
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
