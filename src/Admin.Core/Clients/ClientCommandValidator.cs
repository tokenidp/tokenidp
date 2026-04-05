namespace Admin.Core.Clients;

internal sealed class ClientCommandValidator
{
    private readonly IApplicationDbContext _dbContext;

    public ClientCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> ValidateNewClientIdUniqueAsync(
        int tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Clients
            .AsNoTracking()
            .AnyAsync(c =>
                c.TenantId == tenantId &&
                c.ClientId.ToLower() == clientId.ToLower(),
                cancellationToken);

        return exists
            ? Result.Failure("client.id.duplicate", "Client Id already exists.")
            : Result.Success(0);
    }

    public async Task<Result> ValidateForSaveAsync(
        NormalizedClientCommand command,
        CancellationToken cancellationToken)
    {
        var apiResourceValidationResult = await ValidateApiResourceConfigurationAsync(
            command.TenantId,
            command.ApiResources,
            command.Scopes,
            cancellationToken);
        if (!apiResourceValidationResult.IsSuccess)
        {
            return apiResourceValidationResult;
        }

        var providerValidationResult = await ValidateExternalProvidersAsync(
            command.TenantId,
            command.SelectedProviderIds,
            cancellationToken);
        if (!providerValidationResult.IsSuccess)
        {
            return providerValidationResult;
        }

        return await ValidateProvisioningPolicySettingsAsync(
            command.TenantId,
            command.AuthPolicy.AutoCreateUsers,
            command.AuthPolicy.DefaultRoleId,
            cancellationToken);
    }

    private async Task<Result> ValidateApiResourceConfigurationAsync(
        int tenantId,
        IEnumerable<string> apiResources,
        IEnumerable<string> scopes,
        CancellationToken cancellationToken)
    {
        var requestedApiResources = apiResources.ToArray();
        var requestedApiResourceSet = requestedApiResources.ToHashSet(StringComparer.Ordinal);
        var apiScopeNames = scopes
            .Where(scope => !StandardScopes.Supported.Contains(scope))
            .ToArray();

        var relevantResources = await _dbContext.ApiResources
            .AsNoTracking()
            .Where(resource =>
                resource.TenantId == tenantId &&
                resource.Enabled &&
                (requestedApiResources.Contains(resource.Name) ||
                 resource.Scopes.Any(scope => scope.Enabled && apiScopeNames.Contains(scope.Name))))
            .Select(resource => new
            {
                resource.Name,
                ScopeNames = resource.Scopes
                    .Where(scope => scope.Enabled)
                    .Select(scope => scope.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var availableResourceNames = relevantResources
            .Select(resource => resource.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var apiResource in requestedApiResources)
        {
            if (!availableResourceNames.Contains(apiResource))
            {
                return Result.Failure(
                    "client.api_resource.invalid",
                    $"ApiResource {apiResource} not found or not enabled.");
            }
        }

        var scopeOwnerMap = relevantResources
            .SelectMany(resource => resource.ScopeNames.Select(scopeName => new { scopeName, resource.Name }))
            .ToDictionary(x => x.scopeName, x => x.Name, StringComparer.Ordinal);

        foreach (var scopeName in apiScopeNames)
        {
            if (!scopeOwnerMap.TryGetValue(scopeName, out var apiResourceName))
            {
                return Result.Failure(
                    "invalid_scope",
                    $"Invalid scope: {scopeName} not found or not allowed");
            }

            if (!requestedApiResourceSet.Contains(apiResourceName))
            {
                return Result.Failure(
                    "client.scope.api_resource.not_assigned",
                    $"Scope {scopeName} belongs to ApiResource {apiResourceName} which is not assigned to this client");
            }
        }

        return Result.Success(0);
    }

    private async Task<Result> ValidateExternalProvidersAsync(
        int tenantId,
        IEnumerable<int> providerIds,
        CancellationToken cancellationToken)
    {
        var selectedProviderIds = providerIds.ToArray();

        if (selectedProviderIds.Any(id => id <= 0))
        {
            return Result.Failure(
                "client.external_providers.invalid",
                "One or more selected external providers are invalid.");
        }

        if (selectedProviderIds.Length == 0)
        {
            return Result.Success(0);
        }

        var tenantProviderIds = await _dbContext.TenantExternalProviders
            .AsNoTracking()
            .Where(provider => provider.TenantId == tenantId)
            .Select(provider => provider.Id)
            .ToListAsync(cancellationToken);

        var tenantProviderIdSet = tenantProviderIds.ToHashSet();
        var invalidProviderIds = selectedProviderIds
            .Where(providerId => !tenantProviderIdSet.Contains(providerId))
            .ToArray();

        return invalidProviderIds.Length > 0
            ? Result.Failure(
                "client.external_providers.invalid",
                "One or more selected external providers are invalid for this tenant.")
            : Result.Success(0);
    }

    private async Task<Result> ValidateProvisioningPolicySettingsAsync(
        int tenantId,
        bool autoCreateUsers,
        int? defaultRoleId,
        CancellationToken cancellationToken)
    {
        if (autoCreateUsers && !defaultRoleId.HasValue)
        {
            return Result.Failure(
                "client.auth_policy.default_role.required",
                "A default role is required when auto-create users is enabled.");
        }

        if (!defaultRoleId.HasValue)
        {
            return Result.Success(0);
        }

        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                    r.Id == defaultRoleId.Value &&
                    r.TenantId == tenantId &&
                    !r.IsDeleted,
                cancellationToken);

        if (role is null)
        {
            return Result.Failure(
                "client.auth_policy.default_role.invalid",
                "Selected default role is invalid for this tenant.");
        }

        return !role.IsActive || !role.IsAssignableToNewUsers
            ? Result.Failure(
                "client.auth_policy.default_role.invalid",
                "Selected default role cannot be assigned to new users.")
            : Result.Success(0);
    }
}