namespace TokenIDP.Core.Admin.Clients.UseCases;

internal sealed class ClientCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ClientCommandValidator _validator;
    private readonly IAppLogger<ClientCommandUseCase> _logger;

    public ClientCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ClientCommandValidator validator,
        IAppLogger<ClientCommandUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResult<int>> CreateClient(
        CreateUpdateClient request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        var command = NormalizedClientCommand.Create(request, tenantId);
        var clientId = Guid.NewGuid().ToString();

        _logger.LogDebug("Creating client {ClientId} for tenant {TenantId}", clientId, tenantId);

        var duplicateClientIdResult = await _validator.ValidateNewClientIdUniqueAsync(
            tenantId,
            clientId,
            cancellationToken);
        if (!duplicateClientIdResult.IsSuccess)
        {
            return FailureFromResult(duplicateClientIdResult);
        }

        var createResult = Client.Create(
            tenantId,
            clientId,
            request.ClientName,
            request.Description,
            request.AppType,
            request.AccessTokenType,
            request.RedirectUri,
            request.LogoutRedirectUri,
            request.IsActive,
            request.ClientSecretExpiry,
            request.AccessTokenLifetime,
            request.AuthorizationCodeLifetime,
            request.RefreshTokenExpiration,
            request.PermitLimit,
            request.TimeWindow,
            request.QueueLimit,
            request.EnableITracking,
            out var client);

        if (!createResult.IsSuccess || client == null)
        {
            return FailureFromResult(createResult);
        }

        var applyChangesResult = await PrepareAndApplyAsync(client, command, cancellationToken);
        if (!applyChangesResult.IsSuccess)
        {
            return FailureFromResult(applyChangesResult);
        }

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Client created with Id {ClientId}", client.Id);

        return ApiResult<int>.Success(client.Id);
    }

    public async Task<ApiResult<int>> UpdateClient(
        int id,
        CreateUpdateClient request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        var command = NormalizedClientCommand.Create(request, tenantId);

        _logger.LogDebug("Updating client {ClientId}", id);

        var client = await _dbContext.Clients
            .Include(c => c.ClientScopes)
            .Include(c => c.ClientGrantTypes)
            .Include(c => c.ClientApiResources)
            .Include(c => c.ClientAuthPolicy)
            .Include(c => c.ClientExternalProviders)
            .FirstOrDefaultAsync(c => c.Id == id
                && c.TenantId == tenantId,
                cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found for update: {ClientId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(id)));
        }

        var updateResult = client.UpdateClient(
            request.ClientName,
            request.Description,
            request.AppType,
            request.AccessTokenType,
            request.RedirectUri,
            request.LogoutRedirectUri,
            request.IsActive,
            request.ClientSecretExpiry,
            request.AccessTokenLifetime,
            request.AuthorizationCodeLifetime,
            request.RefreshTokenExpiration,
            request.PermitLimit,
            request.TimeWindow,
            request.QueueLimit,
            request.EnableITracking);

        if (!updateResult.IsSuccess)
        {
            return FailureFromResult(updateResult);
        }

        var applyChangesResult = await PrepareAndApplyAsync(client, command, cancellationToken);
        if (!applyChangesResult.IsSuccess)
        {
            return FailureFromResult(applyChangesResult);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Client updated {ClientId}", id);

        return ApiResult<int>.Success(client.Id);
    }

    public async Task<ApiResult<int>> DeleteClient(
        int clientId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting client {ClientId}", clientId);

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId
                && c.TenantId == _currentUserService.TenantId, cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found for delete: {ClientId}", clientId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(clientId)));
        }

        _dbContext.Clients.Remove(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Client deleted {ClientId}", clientId);

        return ApiResult<int>.Success(clientId);
    }

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }

    private async Task<Result> PrepareAndApplyAsync(
        Client client,
        NormalizedClientCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateForSaveAsync(command, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        var buildChangesResult = ClientCommandMapper.BuildChanges(command, out var changes);
        if (!buildChangesResult.IsSuccess || changes == null)
        {
            return buildChangesResult;
        }

        return ClientCommandMapper.ApplyToClient(client, command, changes);
    }
}
