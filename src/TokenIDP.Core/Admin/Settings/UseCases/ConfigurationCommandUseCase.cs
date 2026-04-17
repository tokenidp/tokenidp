using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Settings.UseCases;

internal sealed class ConfigurationCommandUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ConfigurationCommandUseCase> _logger;

    public ConfigurationCommandUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        IAppLogger<ConfigurationCommandUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<int>> CreateConfiguration(
        CreateUpdateConfiguration request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ConfigKey))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("configuration.key.invalid", "Configuration key is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ConfigValue))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("configuration.value.invalid", "Configuration value is required."));
        }

        var normalizedKey = TenantConfigurationValidation.NormalizeKey(request.ConfigKey);
        var valueValidation = TenantConfigurationValidation.ValidateValue(request.ValueType, request.ConfigValue);
        if (!valueValidation.IsSuccess)
        {
            return ApiResult<int>.Failure(valueValidation.Errors
                .Select(e => ApiError.Failure(e.Code, e.Message))
                .ToList());
        }

        var existing = await _repository.GetByKeyAsync(
            tenantId,
            normalizedKey,
            request.Scope,
            includeDeleted: true,
            cancellationToken);
        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                return ApiResult<int>.Failure(ApiError.Failure("configuration.key.duplicate",
                    "Configuration key already exists for this tenant and scope."));
            }

            var restoreResult = existing.Restore(
                request.ConfigValue,
                request.ValueType,
                request.Scope,
                request.IsEditable);

            if (!restoreResult.IsSuccess)
            {
                return ApiResult<int>.Failure(restoreResult.Errors
                    .Select(e => ApiError.Failure(e.Code, e.Message))
                    .ToList());
            }

            _repository.Update(existing);
            var restored = await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInfo("Configuration restored with Id {ConfigId}", existing.Id);

            return ApiResult<int>.Success(restored);
        }

        var createResult = Configuration.Create(
            tenantId,
            normalizedKey,
            request.ConfigValue,
            request.ValueType,
            request.Scope,
            request.IsEditable,
            out var configuration);

        if (!createResult.IsSuccess || configuration == null)
        {
            return ApiResult<int>.Failure(createResult.Errors
                .Select(e => ApiError.Failure(e.Code, e.Message))
                .ToList());
        }

        await _repository.AddAsync(configuration, cancellationToken);
        var result = await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Configuration created with Id {ConfigId}", configuration.Id);

        return ApiResult<int>.Success(result);
    }
}

