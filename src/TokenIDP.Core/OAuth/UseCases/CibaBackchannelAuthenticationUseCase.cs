using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Core.Foundation.Security;
using TokenIDP.Domain.DomainEvents.Activities;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class CibaBackchannelAuthenticationUseCase
{
    private const int MaxBindingMessageLength = 255;

    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICibaApprovalNotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;
    private readonly CibaOptions _cibaOptions;
    private readonly CibaUserResolver _userResolver;
    private readonly IAppLogger<CibaBackchannelAuthenticationUseCase> _logger;

    public CibaBackchannelAuthenticationUseCase(
        IAuthorizationRepository authorizationRepository,
        IClientRepository clientRepository,
        IUserRepository userRepository,
        ICibaApprovalNotificationService notificationService,
        ICurrentUserService currentUserService,
        IApplicationEventDispatcher applicationEventDispatcher,
        IOptions<CibaOptions> cibaOptions,
        CibaUserResolver userResolver,
        IAppLogger<CibaBackchannelAuthenticationUseCase> logger)
    {
        _authorizationRepository = authorizationRepository;
        _clientRepository = clientRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _applicationEventDispatcher = applicationEventDispatcher;
        _cibaOptions = cibaOptions.Value;
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
        var approvalToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(Math.Max(16, _cibaOptions.ApprovalTokenBytes)));

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

        var tokenCreatedAtUtc = DateTime.UtcNow;
        var configuredTokenLifetimeSeconds = _cibaOptions.ApprovalTokenLifetimeSeconds <= 0
            ? 300
            : _cibaOptions.ApprovalTokenLifetimeSeconds;
        var tokenLifetimeSeconds = Math.Min(configuredTokenLifetimeSeconds, expiresIn);
        entity.SetApprovalChallenge(
            Guid.NewGuid(),
            SecretHasher.HashSecret(approvalToken),
            tokenCreatedAtUtc,
            tokenCreatedAtUtc.AddSeconds(tokenLifetimeSeconds),
            resolvedUser.HintValueHash);

        await _authorizationRepository.CreateBackchannelAuthenticationRequest(entity, ct);

        _logger.LogInfo(
            "Created CIBA request. RequestId={RequestId}, TenantId={TenantId}, ClientId={ClientId}, UserId={UserId}",
            entity.Id,
            entity.TenantId,
            entity.ClientId,
            entity.UserId ?? 0);

        await SendApprovalNotificationAsync(entity, client, resolvedUser.UserId, approvalToken, ct);

        return new CibaBackchannelAuthenticationResponse
        {
            AuthReqId = authReqId,
            ExpiresIn = expiresIn,
            Interval = client.CibaMinIntervalSeconds
        };
    }

    private async Task SendApprovalNotificationAsync(
        TokenIDP.Domain.AggregateRoots.Authorization.BackchannelAuthenticationRequest entity,
        ClientValidationSnapshot client,
        int userId,
        string approvalToken,
        CancellationToken ct)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            var approvalUrl = BuildApprovalUrl(entity.PublicRequestId, approvalToken);

            await _notificationService.SendApprovalRequestAsync(
                new CibaApprovalNotification
                {
                    TenantId = entity.TenantId,
                    UserId = userId,
                    UserEmail = user.Email ?? string.Empty,
                    ClientName = client.ClientName,
                    BindingMessage = entity.BindingMessage ?? string.Empty,
                    ApprovalUrl = approvalUrl,
                    ExpiresAtUtc = entity.ExpiresAtUtc,
                    RequestedScopes = entity.RequestedScopes
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                },
                ct);

            entity.MarkApprovalLinkSent(DateTime.UtcNow);
            await _authorizationRepository.UpdateBackchannelAuthenticationRequest(entity, ct);

            await _applicationEventDispatcher.RaiseAsync(
                CreateActivity(
                    entity,
                    ActivityEventType.CibaApprovalEmailSent,
                    "Sent",
                    $"CIBA approval email sent for {client.ClientName}."),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CIBA approval notification failed. RequestId={RequestId}, TenantId={TenantId}, ClientId={ClientId}",
                entity.Id,
                entity.TenantId,
                entity.ClientId);

            if (_cibaOptions.RequireNotificationDelivery)
            {
                throw new BackchannelAuthenticationValidationException(
                    "server_error",
                    "CIBA approval notification could not be delivered.");
            }
        }
    }

    private string BuildApprovalUrl(Guid publicRequestId, string approvalToken)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_cibaOptions.ApprovalBaseUrl)
            ? _cibaOptions.ApprovalBaseUrl!.TrimEnd('/')
            : _currentUserService.BaseUrl.TrimEnd('/');

        return QueryHelpers.AddQueryString(
            $"{baseUrl}/ciba/approve",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["requestId"] = publicRequestId.ToString("D"),
                ["token"] = approvalToken
            });
    }

    private static ActivityDomainEvent CreateActivity(
        TokenIDP.Domain.AggregateRoots.Authorization.BackchannelAuthenticationRequest request,
        ActivityEventType eventType,
        string status,
        string description)
        => new(
            TenantId: request.TenantId,
            EventType: eventType,
            AggregateType: "CibaRequest",
            AggregateId: request.PublicRequestId.ToString("D"),
            ActorId: request.UserId?.ToString(),
            ActorDisplayName: null,
            TargetId: request.PublicRequestId.ToString("D"),
            TargetDescription: request.ClientId,
            Status: status,
            Description: description);

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
