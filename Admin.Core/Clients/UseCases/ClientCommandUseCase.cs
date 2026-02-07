using IDP.Foundation.Security;

namespace Admin.Core.Clients.UseCases;

internal sealed class ClientCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<CreateUpdateClient> _logger;

    public ClientCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<CreateUpdateClient> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<int>> CreateClient(
        CreateUpdateClient request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating client {ClientId} for tenant {TenantId}",
            request.ClientId, _currentUserService.TenantId);

        var tenantId = _currentUserService.TenantId;

        var existing = await _dbContext.Clients
            .AsNoTracking()
            .AnyAsync(c => c.TenantId == tenantId
                && c.ClientId.ToLower() == request.ClientId.ToLower(),
                cancellationToken);

        if (existing)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("client.id.duplicate", "Client Id already exists."));
        }

        var createResult = Client.Create(
            tenantId,
            request.ClientId,
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

        var scopeResult = BuildScopes(request.Scopes, out var scopes);
        if (!scopeResult.IsSuccess)
        {
            return FailureFromResult(scopeResult);
        }

        var grantResult = BuildGrantTypes(request.GrantTypes, out var grants);
        if (!grantResult.IsSuccess)
        {
            return FailureFromResult(grantResult);
        }

        var audienceResult = BuildAudiences(request.Audiences, out var audiences);
        if (!audienceResult.IsSuccess)
        {
            return FailureFromResult(audienceResult);
        }

        var secretResult = BuildSecret(
            request.ClientSecret,
            request.ClientSecretDescription,
            request.ClientSecretExpiry,
            out var clientSecret);
        if (!secretResult.IsSuccess)
        {
            return FailureFromResult(secretResult);
        }

        client.ReplaceScopes(scopes);
        client.ReplaceGrantTypes(grants);
        client.ReplaceAudiences(audiences);
        if (clientSecret != null)
        {
            client.AddSecret(clientSecret);
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
        _logger.LogDebug("Updating client {ClientId}", id);

        var client = await _dbContext.Clients
            .Include(c => c.ClientScopes)
            .Include(c => c.ClientGrantTypes)
            .Include(c => c.ClientAudiences)
            .FirstOrDefaultAsync(c => c.Id == id
                && c.TenantId == _currentUserService.TenantId,
                cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found for update: {ClientId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(id)));
        }

        var updateResult = client.UpdateClient(
            request.ClientId,
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

        var scopeResult = BuildScopes(request.Scopes, out var scopes);
        if (!scopeResult.IsSuccess)
        {
            return FailureFromResult(scopeResult);
        }

        var grantResult = BuildGrantTypes(request.GrantTypes, out var grants);
        if (!grantResult.IsSuccess)
        {
            return FailureFromResult(grantResult);
        }

        var audienceResult = BuildAudiences(request.Audiences, out var audiences);
        if (!audienceResult.IsSuccess)
        {
            return FailureFromResult(audienceResult);
        }

        var secretResult = BuildSecret(
            request.ClientSecret,
            request.ClientSecretDescription,
            request.ClientSecretExpiry,
            out var clientSecret);
        if (!secretResult.IsSuccess)
        {
            return FailureFromResult(secretResult);
        }

        client.ReplaceScopes(scopes);
        client.ReplaceGrantTypes(grants);
        client.ReplaceAudiences(audiences);
        if (clientSecret != null)
        {
            client.AddSecret(clientSecret);
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

    private static Result BuildSecret(
        string? secret,
        string? description,
        int? clientSecretExpiry,
        out ClientSecret? mapped)
    {
        mapped = null;

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result.Success(0);
        }

        var hash = SecretHasher.HashSecret(secret);
        var expiresAt = clientSecretExpiry.HasValue
            ? DateTime.UtcNow.AddDays(clientSecretExpiry.Value)
            : (DateTime?)null;

        return ClientSecret.Create(hash, description, expiresAt, out mapped);
    }

    private static ApiResult<int> FailureFromResult(Result result)
    {
        return ApiResult<int>.Failure(
            result.Errors.Select(e => ApiError.Failure(e.Code, e.Message)).ToList());
    }

    private static Result BuildScopes(IEnumerable<string> scopes, out List<ClientScope> mapped)
    {
        mapped = new List<ClientScope>();

        if (scopes == null)
        {
            return Result.Success(0);
        }

        var combined = Result.Success(0);
        foreach (var scope in scopes)
        {
            var result = ClientScope.Create(scope, out var created);
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

    private static Result BuildGrantTypes(IEnumerable<GrantTypes> grantTypes, out List<ClientGrantType> mapped)
    {
        mapped = new List<ClientGrantType>();

        if (grantTypes == null)
        {
            return Result.Success(0);
        }

        var combined = Result.Success(0);
        foreach (var grantType in grantTypes)
        {
            var result = ClientGrantType.Create(grantType, out var created);
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

    private static Result BuildAudiences(IEnumerable<string> audiences, out List<ClientAudience> mapped)
    {
        mapped = new List<ClientAudience>();

        if (audiences == null)
        {
            return Result.Success(0);
        }

        var combined = Result.Success(0);
        foreach (var audience in audiences)
        {
            var result = ClientAudience.Create(audience, true, out var created);
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
}
