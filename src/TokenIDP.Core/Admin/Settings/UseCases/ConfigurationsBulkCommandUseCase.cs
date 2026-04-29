using System.Transactions;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Settings.UseCases;

internal sealed class ConfigurationsBulkCommandUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICache _cache;
    private readonly IAppLogger<ConfigurationsBulkCommandUseCase> _logger;

    public ConfigurationsBulkCommandUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        ICache cache,
        IAppLogger<ConfigurationsBulkCommandUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResult<BulkUpdateTenantConfigurationsResult>> BulkUpdate(
        BulkUpdateTenantConfigurations request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        if (tenantId <= 0)
        {
            return ApiResult<BulkUpdateTenantConfigurationsResult>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return ApiResult<BulkUpdateTenantConfigurationsResult>.Failure(
                ApiError.Failure("configuration.bulk.empty", "No configuration changes provided."));
        }

        var validationErrors = new List<ApiError>();
        var normalizedItems = new List<(BulkTenantConfigurationItem Item, string NormalizedKey)>();

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                validationErrors.Add(ApiError.Failure("configuration.key.invalid", "Configuration key is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Value))
            {
                validationErrors.Add(ApiError.Failure("configuration.value.invalid", "Configuration value is required."));
                continue;
            }

            normalizedItems.Add((item, TenantConfigurationValidation.NormalizeKey(item.Key)));
        }

        if (validationErrors.Count > 0)
        {
            return ApiResult<BulkUpdateTenantConfigurationsResult>.Failure(validationErrors);
        }

        var duplicateKeys = normalizedItems
            .GroupBy(x => x.NormalizedKey)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateKeys.Count > 0)
        {
            return ApiResult<BulkUpdateTenantConfigurationsResult>.Failure(
                ApiError.Failure("configuration.key.duplicate",
                    "Duplicate configuration keys found: {0}".FormatString(string.Join(", ", duplicateKeys))));
        }

        foreach (var entry in normalizedItems)
        {
            var validation = TenantConfigurationValidation.ValidateValue(entry.Item.ValueType, entry.Item.Value);
            if (!validation.IsSuccess)
            {
                validationErrors.AddRange(validation.Errors.Select(e =>
                    ApiError.Failure(e.Code, "{0}: {1}".FormatString(entry.NormalizedKey, e.Message))));
            }
        }

        if (validationErrors.Count > 0)
        {
            return ApiResult<BulkUpdateTenantConfigurationsResult>.Failure(validationErrors);
        }

        var requestedKeys = normalizedItems
            .Select(x => x.NormalizedKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var requestedPairs = normalizedItems
            .Select(x => GetLookupKey(x.NormalizedKey, x.Item.Scope))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingConfigurations = await _repository.Query()
            .Where(c => c.TenantId == tenantId && requestedKeys.Contains(c.ConfigKey))
            .ToListAsync(cancellationToken);

        var existingLookup = existingConfigurations
            .Where(c => requestedPairs.Contains(GetLookupKey(c.ConfigKey, c.Scope)))
            .ToDictionary(c => GetLookupKey(c.ConfigKey, c.Scope), StringComparer.OrdinalIgnoreCase);
        var result = new BulkUpdateTenantConfigurationsResult
        {
            Requested = request.Items.Count
        };

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        foreach (var entry in normalizedItems)
        {
            var lookupKey = GetLookupKey(entry.NormalizedKey, entry.Item.Scope);
            if (existingLookup.TryGetValue(lookupKey, out var existing))
            {
                if (existing.IsDeleted)
                {
                    var restoreResult = existing.Restore(
                        entry.Item.Value,
                        entry.Item.ValueType,
                        entry.Item.Scope,
                        entry.Item.IsEditable);

                    if (!restoreResult.IsSuccess)
                    {
                        validationErrors.AddRange(restoreResult.Errors.Select(e =>
                            ApiError.Failure(e.Code, "{0}: {1}".FormatString(entry.NormalizedKey, e.Message))));
                        continue;
                    }

                    _repository.Update(existing);
                    result.Updated++;
                    continue;
                }

                if (!existing.IsEditable)
                {
                    validationErrors.Add(ApiError.Failure("configuration.readonly",
                        "Configuration {0} is read-only.".FormatString(entry.NormalizedKey)));
                    continue;
                }

                var updateResult = existing.UpdateConfiguration(
                    entry.Item.Value,
                    entry.Item.ValueType,
                    entry.Item.Scope,
                    entry.Item.IsEditable);

                if (!updateResult.IsSuccess)
                {
                    validationErrors.AddRange(updateResult.Errors.Select(e =>
                        ApiError.Failure(e.Code, "{0}: {1}".FormatString(entry.NormalizedKey, e.Message))));
                    continue;
                }

                _repository.Update(existing);
                result.Updated++;
                continue;
            }

            var createResult = Configuration.Create(
                tenantId,
                entry.NormalizedKey,
                entry.Item.Value,
                entry.Item.ValueType,
                entry.Item.Scope,
                entry.Item.IsEditable,
                out var configuration);

            if (!createResult.IsSuccess || configuration == null)
            {
                validationErrors.AddRange(createResult.Errors.Select(e =>
                    ApiError.Failure(e.Code, "{0}: {1}".FormatString(entry.NormalizedKey, e.Message))));
                continue;
            }

            await _repository.AddAsync(configuration, cancellationToken);
            result.Created++;
        }

        if (validationErrors.Count > 0)
        {
            return ApiResult<BulkUpdateTenantConfigurationsResult>.Failure(validationErrors);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        scope.Complete();

        foreach (var key in requestedKeys)
        {
            var cacheKey = CacheKeys.CONFIGURATION.FormatCacheKey("Key", tenantId, key);
            await _cache.RemoveAsync(cacheKey);
        }

        _logger.LogInfo("Bulk updated {Count} configurations for tenant {TenantId}",
            request.Items.Count, tenantId);

        return ApiResult<BulkUpdateTenantConfigurationsResult>.Success(result);
    }

    private static string GetLookupKey(string key, ConfigurationScopes scope)
    {
        return $"{scope}:{key}";
    }
}

