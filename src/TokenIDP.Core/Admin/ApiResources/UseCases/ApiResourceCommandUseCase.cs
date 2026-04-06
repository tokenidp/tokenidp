namespace TokenIDP.Core.Admin.ApiResources.UseCases;

internal sealed class ApiResourceCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ApiResourceCommandUseCase> _logger;

    public ApiResourceCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<ApiResourceCommandUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<Guid>> CreateAsync(
        CreateUpdateApiResource request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;

        var duplicateName = await _dbContext.ApiResources
            .AsNoTracking()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Name.ToLower() == request.Name.Trim().ToLower(),
                cancellationToken);

        if (duplicateName)
        {
            return ApiResult<Guid>.Failure(
                ApiError.Failure("api_resource.name.duplicate", "ApiResource name must be unique per tenant."));
        }

        var scopeValidation = ValidateScopeDefinitions(request.Scopes);
        if (!scopeValidation.IsSuccess)
        {
            return FailureFromResult(scopeValidation);
        }

        var createResult = ApiResource.Create(
            tenantId,
            request.Name,
            request.DisplayName,
            request.Description,
            request.Enabled,
            out var apiResource);

        if (!createResult.IsSuccess || apiResource == null)
        {
            return FailureFromResult(createResult);
        }

        var buildScopesResult = BuildScopes(request.Scopes, out var scopes);
        if (!buildScopesResult.IsSuccess)
        {
            return FailureFromResult(buildScopesResult);
        }

        apiResource.ReplaceScopes(scopes);
        _dbContext.ApiResources.Add(apiResource);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<Guid>.Success(apiResource.Id);
    }

    public async Task<ApiResult<Guid>> UpdateAsync(
        Guid id,
        CreateUpdateApiResource request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;

        var apiResource = await _dbContext.ApiResources
            .Include(x => x.Scopes)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (apiResource == null)
        {
            return ApiResult<Guid>.Failure(
                ApiError.Failure("api_resource.not_found", $"ApiResource not found for Id {id}"));
        }

        var duplicateName = await _dbContext.ApiResources
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id != id &&
                x.TenantId == tenantId &&
                x.Name.ToLower() == request.Name.Trim().ToLower(),
                cancellationToken);

        if (duplicateName)
        {
            return ApiResult<Guid>.Failure(
                ApiError.Failure("api_resource.name.duplicate", "ApiResource name must be unique per tenant."));
        }

        var scopeValidation = ValidateScopeDefinitions(request.Scopes);
        if (!scopeValidation.IsSuccess)
        {
            return FailureFromResult(scopeValidation);
        }

        var existingScopes = apiResource.Scopes.ToDictionary(x => x.Id, x => x);
        var requestedScopeIds = request.Scopes
            .Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        var removedScopes = apiResource.Scopes
            .Where(x => !requestedScopeIds.Contains(x.Id))
            .Select(x => x.Name)
            .ToArray();

        if (removedScopes.Length > 0)
        {
            var assignedScope = await (
                from clientScope in _dbContext.ClientScopes.AsNoTracking()
                join client in _dbContext.Clients.AsNoTracking() on clientScope.ClientId equals client.Id
                where client.TenantId == tenantId && removedScopes.Contains(clientScope.Scope)
                select clientScope.Scope
            ).FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(assignedScope))
            {
                return ApiResult<Guid>.Failure(
                    ApiError.Failure(
                        "api_scope.delete.blocked",
                        $"Cannot delete scope {assignedScope} because it is assigned to one or more clients."));
            }
        }

        var oldName = apiResource.Name;
        var updateResult = apiResource.Update(
            request.Name,
            request.DisplayName,
            request.Description,
            request.Enabled);

        if (!updateResult.IsSuccess)
        {
            return FailureFromResult(updateResult);
        }

        if (!string.Equals(oldName, apiResource.Name, StringComparison.Ordinal))
        {
            var clientAssignments = await (
                from clientApiResource in _dbContext.ClientApiResources
                join client in _dbContext.Clients on clientApiResource.ClientId equals client.Id
                where client.TenantId == tenantId && clientApiResource.Name == oldName
                select clientApiResource
            ).ToListAsync(cancellationToken);

            foreach (var assignment in clientAssignments)
            {
                assignment.Rename(apiResource.Name);
            }
        }

        foreach (var requestedScope in request.Scopes.Where(x => x.Id.HasValue && existingScopes.ContainsKey(x.Id.Value)))
        {
            var existingScope = existingScopes[requestedScope.Id!.Value];
            var previousName = existingScope.Name;
            var scopeUpdateResult = existingScope.Update(
                requestedScope.Name,
                requestedScope.DisplayName,
                requestedScope.Description,
                requestedScope.Enabled);

            if (!scopeUpdateResult.IsSuccess)
            {
                return FailureFromResult(scopeUpdateResult);
            }

            if (!string.Equals(previousName, existingScope.Name, StringComparison.Ordinal))
            {
                var assignedScopes = await (
                    from clientScope in _dbContext.ClientScopes
                    join client in _dbContext.Clients on clientScope.ClientId equals client.Id
                    where client.TenantId == tenantId && clientScope.Scope == previousName
                    select clientScope
                ).ToListAsync(cancellationToken);

                foreach (var clientScope in assignedScopes)
                {
                    clientScope.Rename(existingScope.Name);
                }
            }
        }

        var replacementScopes = new List<ApiScope>();
        foreach (var requestedScope in request.Scopes)
        {
            if (requestedScope.Id.HasValue 
                && requestedScope.Id.Value != Guid.Empty 
                && existingScopes.TryGetValue(requestedScope.Id.Value, out var existingScope))
            {
                replacementScopes.Add(existingScope);
                continue;
            }

            var createScopeResult = ApiScope.Create(
                requestedScope.Name,
                requestedScope.DisplayName,
                requestedScope.Description,
                requestedScope.Enabled,
                out var newScope);

            if (!createScopeResult.IsSuccess || newScope == null)
            {
                return FailureFromResult(createScopeResult);
            }

            replacementScopes.Add(newScope);
        }

        apiResource.ReplaceScopes(replacementScopes);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<Guid>.Success(apiResource.Id);
    }

    public async Task<ApiResult<Guid>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;

        var apiResource = await _dbContext.ApiResources
            .Include(x => x.Scopes)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (apiResource == null)
        {
            return ApiResult<Guid>.Failure(
                ApiError.Failure("api_resource.not_found", $"ApiResource not found for Id {id}"));
        }

        if (apiResource.Scopes.Count > 0)
        {
            return ApiResult<Guid>.Failure(
                ApiError.Failure(
                    "api_resource.delete.blocked",
                    "Cannot delete ApiResource if it has assigned scopes or clients"));
        }

        var hasAssignedClients = await (
            from clientApiResource in _dbContext.ClientApiResources.AsNoTracking()
            join client in _dbContext.Clients.AsNoTracking() on clientApiResource.ClientId equals client.Id
            where client.TenantId == tenantId && clientApiResource.Name == apiResource.Name
            select clientApiResource.Id
        ).AnyAsync(cancellationToken);

        if (hasAssignedClients)
        {
            return ApiResult<Guid>.Failure(
                ApiError.Failure(
                    "api_resource.delete.blocked",
                    "Cannot delete ApiResource if it has assigned scopes or clients"));
        }

        _dbContext.ApiResources.Remove(apiResource);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<Guid>.Success(id);
    }

    private static Result ValidateScopeDefinitions(IEnumerable<CreateUpdateApiScope>? scopes)
    {
        var combined = Result.Success(0);
        var scopeItems = (scopes ?? Array.Empty<CreateUpdateApiScope>()).ToList();

        var duplicateNames = scopeItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            combined = combined.Combine(Result.Failure(
                "api_scope.name.duplicate",
                $"Scope names must be unique per ApiResource. Duplicate scopes: {string.Join(", ", duplicateNames)}"));
        }

        return combined;
    }

    private static Result BuildScopes(IEnumerable<CreateUpdateApiScope>? scopes, out List<ApiScope> mapped)
    {
        mapped = new List<ApiScope>();
        var combined = Result.Success(0);

        foreach (var scope in scopes ?? Array.Empty<CreateUpdateApiScope>())
        {
            var result = ApiScope.Create(
                scope.Name,
                scope.DisplayName,
                scope.Description,
                scope.Enabled,
                out var created);

            if (!result.IsSuccess)
            {
                combined = combined.Combine(result);
                continue;
            }

            if (created != null)
            {
                mapped.Add(created);
            }
        }

        return combined;
    }

    private static ApiResult<Guid> FailureFromResult(Result result)
    {
        return ApiResult<Guid>.Failure(
            result.Errors.Select(error => ApiError.Failure(error.Code, error.Message)).ToList());
    }
}

