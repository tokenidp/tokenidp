using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Security;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class CibaBackchannelAuthenticationUseCase
{
    private const int MaxBindingMessageLength = 255;

    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly CibaUserResolver _userResolver;
    private readonly IAppLogger<CibaBackchannelAuthenticationUseCase> _logger;

    public CibaBackchannelAuthenticationUseCase(
        IAuthorizationRepository authorizationRepository,
        IClientRepository clientRepository,
        CibaUserResolver userResolver,
        IAppLogger<CibaBackchannelAuthenticationUseCase> logger)
    {
        _authorizationRepository = authorizationRepository;
        _clientRepository = clientRepository;
        _userResolver = userResolver;
        _logger = logger;
    }

    public async Task<CibaBackchannelAuthenticationResponse> CreateAsync(
        CibaBackchannelAuthenticationRequest request,
        CancellationToken ct)
    {
        var client = await _clientRepository.GetActiveByClientId(request.ClientId);

        ValidateClientConfiguration(client);
        ValidateScope(client, request.Scope);
        ValidateBindingMessage(request.BindingMessage);

        var resolvedUser = await _userResolver.ResolveAsync(client, request, ct);

        if (resolvedUser.TenantId != request.TenantId)
        {
            throw new BackchannelAuthenticationValidationException(
                "unknown_user_id",
                "The provided user hint could not be resolved.");
        }

        ValidateUserCode(client, request, resolvedUser);

        var requestedExpiry = request.RequestedExpiry;
        if (requestedExpiry.HasValue && requestedExpiry.Value <= 0)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_request",
                "requested_expiry must be a positive integer.");
        }

        var expiresIn = requestedExpiry.HasValue
            ? Math.Min(requestedExpiry.Value, client.CibaDefaultExpirySeconds)
            : client.CibaDefaultExpirySeconds;

        var authReqId = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var authReqIdHash = SecretHasher.HashSecret(authReqId);

        var entity = TokenIDP.Domain.AggregateRoots.Authorization.BackchannelAuthenticationRequest.Create(
            request.TenantId,
            request.ClientId,
            resolvedUser.UserId,
            request.Scope,
            resolvedUser.HintType,
            resolvedUser.HintValueHash,
            resolvedUser.SubjectHint,
            request.BindingMessage,
            string.IsNullOrWhiteSpace(request.UserCode) ? null : SecretHasher.HashSecret(request.UserCode),
            authReqIdHash,
            TokenIDP.Domain.AggregateRoots.Authorization.CibaDeliveryMode.Poll,
            requestedExpiry,
            DateTime.UtcNow.AddSeconds(expiresIn),
            client.CibaMinIntervalSeconds,
            null,
            request.AcrValues);

        await _authorizationRepository.CreateBackchannelAuthenticationRequest(entity, ct);

        _logger.LogInfo(
            "Created CIBA request. RequestId={RequestId}, TenantId={TenantId}, ClientId={ClientId}, UserId={UserId}",
            entity.Id,
            entity.TenantId,
            entity.ClientId,
            entity.UserId ?? 0);

        return new CibaBackchannelAuthenticationResponse
        {
            AuthReqId = authReqId,
            ExpiresIn = expiresIn,
            Interval = client.CibaMinIntervalSeconds
        };
    }

    private static void ValidateClientConfiguration(ClientValidationSnapshot client)
    {
        if (!client.CibaEnabled ||
            !client.GrantTypes.Contains(GrantTypes.ciba))
        {
            throw new BackchannelAuthenticationValidationException(
                "unauthorized_client",
                "The client is not authorized to use CIBA.");
        }

        if (client.BackchannelTokenDeliveryMode != CibaTokenDeliveryModes.Poll)
        {
            throw new BackchannelAuthenticationValidationException(
                "unauthorized_client",
                "Only Poll delivery mode is currently supported.");
        }
    }

    private static void ValidateScope(ClientValidationSnapshot client, string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_request",
                "scope is required.");
        }

        var requestedScopes = scope
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        if (!requestedScopes.Contains("openid"))
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_scope",
                "Missing 'openid' scope.");
        }

        var invalidScopes = requestedScopes
            .Where(x => !client.Scopes.Contains(x))
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_scope",
                $"Invalid scope: {invalidScopes[0]}.");
        }
    }

    private static void ValidateBindingMessage(string? bindingMessage)
    {
        if (string.IsNullOrWhiteSpace(bindingMessage))
        {
            return;
        }

        if (bindingMessage.Length > MaxBindingMessageLength ||
            bindingMessage.Any(ch => ch < 0x20 || ch > 0x7E))
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_binding_message",
                "The provided binding_message is invalid.");
        }
    }

    private static void ValidateUserCode(
        ClientValidationSnapshot client,
        CibaBackchannelAuthenticationRequest request,
        CibaUserResolver.CibaResolvedUser resolvedUser)
    {
        if (!client.RequireCibaUserCode)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.UserCode))
        {
            throw new BackchannelAuthenticationValidationException(
                "missing_user_code",
                "user_code is required for this client.");
        }

        if (!string.Equals(request.UserCode, resolvedUser.ExpectedUserCode, StringComparison.Ordinal))
        {
            throw new BackchannelAuthenticationValidationException(
                "invalid_user_code",
                "The provided user_code is invalid.");
        }
    }
}
