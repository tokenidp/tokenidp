using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace TokenIDP.Core.Admin.Clients.UseCases;

internal sealed class ClientCommandUseCase
{
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ClientCommandValidator _validator;
    private readonly IAppLogger<ClientCommandUseCase> _logger;

    public ClientCommandUseCase(
        IClientRepository clientRepository,
        ICurrentUserService currentUserService,
        ClientCommandValidator validator,
        IAppLogger<ClientCommandUseCase> logger)
    {
        _clientRepository = clientRepository;
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
            request.CibaEnabled,
            request.BackchannelTokenDeliveryMode,
            request.CibaDefaultExpirySeconds,
            request.CibaMinIntervalSeconds,
            request.RequireCibaUserCode,
            request.AllowCibaLoginHint,
            request.AllowCibaLoginHintToken,
            request.AllowCibaIdTokenHint,
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

        await _clientRepository.AddAsync(client, cancellationToken);

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

        var client = await _clientRepository.GetClientAggregateAsync(id, tenantId, cancellationToken);

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
            request.EnableITracking,
            request.CibaEnabled,
            request.BackchannelTokenDeliveryMode,
            request.CibaDefaultExpirySeconds,
            request.CibaMinIntervalSeconds,
            request.RequireCibaUserCode,
            request.AllowCibaLoginHint,
            request.AllowCibaLoginHintToken,
            request.AllowCibaIdTokenHint);

        if (!updateResult.IsSuccess)
        {
            return FailureFromResult(updateResult);
        }

        var applyChangesResult = await PrepareAndApplyAsync(client, command, cancellationToken);
        if (!applyChangesResult.IsSuccess)
        {
            return FailureFromResult(applyChangesResult);
        }

        await _clientRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Client updated {ClientId}", id);

        return ApiResult<int>.Success(client.Id);
    }

    public async Task<ApiResult<int>> DeleteClient(
        int clientId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting client {ClientId}", clientId);

        var client = await _clientRepository.GetClientAggregateAsync(
            clientId,
            _currentUserService.TenantId,
            cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found for delete: {ClientId}", clientId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(clientId)));
        }

        await _clientRepository.DeleteAsync(client, cancellationToken);

        _logger.LogInfo("Client deleted {ClientId}", clientId);

        return ApiResult<int>.Success(clientId);
    }

    public async Task<ApiResult<RotateClientSecretResponse>> RotateClientSecret(
        int clientId,
        RotateClientSecretRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Rotating client secret for client {ClientId}", clientId);

        var client = await _clientRepository.GetClientAggregateAsync(
            clientId,
            _currentUserService.TenantId,
            cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found for secret rotation: {ClientId}", clientId);
            return ApiResult<RotateClientSecretResponse>.Failure(ApiError.Failure(
                "NotFound",
                "Client not found for the Id {0}".FormatString(clientId)));
        }

        if (!client.RequiresClientSecret())
        {
            return ApiResult<RotateClientSecretResponse>.Failure(ApiError.Failure(
                "client.secret.unsupported",
                "Client secrets are supported for WebApp and Backend clients only."));
        }

        client.RevokeActiveSecrets();

        var rawSecret = GenerateClientSecret();
        var expiresAt = request.ClientSecretExpiry.HasValue
            ? DateTime.UtcNow.AddDays(request.ClientSecretExpiry.Value)
            : (DateTime?)null;

        var createSecretResult = ClientSecret.Create(
            SecretHasher.HashSecret(rawSecret),
            description: "Rotated via admin portal",
            expiresAt,
            out var clientSecret);

        if (!createSecretResult.IsSuccess || clientSecret == null)
        {
            return ApiResult<RotateClientSecretResponse>.Failure(
                createSecretResult.Errors
                    .Select(e => ApiError.Failure(e.Code, e.Message))
                    .ToList());
        }

        var addSecretResult = client.AddSecret(clientSecret);
        if (!addSecretResult.IsSuccess)
        {
            return ApiResult<RotateClientSecretResponse>.Failure(
                addSecretResult.Errors
                    .Select(e => ApiError.Failure(e.Code, e.Message))
                    .ToList());
        }

        await _clientRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Client secret rotated for client {ClientId}", clientId);

        return ApiResult<RotateClientSecretResponse>.Success(new RotateClientSecretResponse
        {
            ClientSecret = rawSecret,
            ClientSecretExpiry = request.ClientSecretExpiry
        });
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

    private static string GenerateClientSecret()
    {
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(secretBytes);
    }
}
