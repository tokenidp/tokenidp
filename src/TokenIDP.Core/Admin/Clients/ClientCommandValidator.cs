using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Domain;

namespace TokenIDP.Core.Admin.Clients;

internal sealed class ClientCommandValidator
{
    private readonly IApiResourceRepository _apiResourceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantRepository _tenantRepository;

    public ClientCommandValidator(
        IClientRepository clientRepository,
        IApiResourceRepository apiResourceRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository)
    {
        _clientRepository = clientRepository;
        _apiResourceRepository = apiResourceRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;
    }

    public async Task<Result> ValidateNewClientIdUniqueAsync(
        int tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetTenantDetailAsync(tenantId, cancellationToken);
        var isSystemTenant = tenant?.IsSystemTenant == true;

        if (SystemIdentity.IsReservedSystemClientId(clientId) && !isSystemTenant)
        {
            return Result.Failure(
                "client.id.reserved",
                "Client Id is reserved for the system tenant.");
        }

        if (SystemIdentity.IsReservedSystemClientId(clientId))
        {
            var existsGlobally = await _clientRepository.ClientIdExistsGloballyAsync(
                clientId,
                cancellationToken);

            return existsGlobally
                ? Result.Failure("client.id.duplicate", "Client Id already exists.")
                : Result.Success(0);
        }

        var exists = await _clientRepository.ClientIdExistsAsync(
            tenantId,
            clientId,
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

        var relevantResources = await _apiResourceRepository.GetEnabledApiResourcesAsync(
            tenantId,
            requestedApiResources,
            apiScopeNames,
            cancellationToken);

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

        var tenantProviderIdSet = await _tenantRepository.GetTenantExternalProviderIdsAsync(
            tenantId,
            cancellationToken);
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

        var role = await _roleRepository.GetRoleAssignmentValidationAsync(
            tenantId,
            defaultRoleId.Value,
            cancellationToken);

        if (role is null || !role.Exists)
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
