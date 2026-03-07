using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Emails;
using IDP.Domain.AggregateRoots.Emails.ValueObjects;
using IDP.Foundation.Abstractions.Stores;
using System.Text.Json;

namespace IDP.Core.UseCases;

internal sealed class MfaUseCase : IMfaUseCase
{
    private readonly IUserStore _identityStore;
    private readonly IEmailQueueStore _emailQueueStore;
    private readonly IAuthorizationStore _preAuthorizationRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<MfaUseCase> _logger;

    public MfaUseCase(IUserStore identityStore,
        IAuthorizationStore preAuthorizationRepo,
        IAppLogger<MfaUseCase> logger,
        ICurrentUserService currentUserService,
        IEmailQueueStore emailQueueStore)
    {
        _logger = logger;
        _preAuthorizationRepo = preAuthorizationRepo;
        _identityStore = identityStore;
        _currentUserService = currentUserService;
        _emailQueueStore = emailQueueStore;
    }

    public async Task<AuthorizationResponse> GenerateMfaForAuthorizeAsync(AuthorizationRequest request,
        int userId,
        CancellationToken ct = default)
    {
        var preAuthorization = await _preAuthorizationRepo
           .GetPreAuthorization(request.AuthorizationContextId);

        if (preAuthorization is null)
        {
            return AuthorizationResponse.Failure("Authorization context is invalid or expired.");
        }

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        preAuthorization.UpdateMfaCode(userId, mfaCode, DateTime.UtcNow.AddMinutes(5));

        await _preAuthorizationRepo.UpdatePreAuthorization(preAuthorization);

        return await CompleteMfaProcess(userId, mfaCode, request.AuthorizationContextId);
    }

    public async Task<AuthorizationResponse> GenerateMfaCode(GenerateMfaCommand command,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogDebug(
            "MFA challenge started. CorrelationId: {CorrelationId}, UserId: {UserId}",
            correlationId,
            command.UserId);

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        var preAuthorization = new PreAuthorization(
            command.TenantId,
            correlationId,
            command.ClientId,
            mfaCode,
            command.Scopes,
            expiresAt,
            command.UserId);

        await _preAuthorizationRepo.CreatePreAuthorization(preAuthorization, ct);

        return await CompleteMfaProcess(command.UserId, mfaCode, correlationId);
    }

    public async Task<(AuthorizationRequest?, AuthorizationResponse)> VerifyMfaRequest(MfaRequest request)
    {
        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId} and Code:{request.Code}";
            _logger.LogWarning(message);
            return (default, AuthorizationResponse.Failure(message));
        }

        var authResponse = AuthorizationResponse.Success(preAuthorization.UserId ?? 0, preAuthorization.CorrelationId, true);

        var authRequest = AuthorizationRequest.Create(preAuthorization.ClientId ?? string.Empty,
            preAuthorization.RedirectUri ?? string.Empty,
            preAuthorization.CodeChallenge ?? string.Empty,
            preAuthorization.CodeChallengeMethod ?? string.Empty,
            preAuthorization.Scopes ?? string.Empty);

        var user = await _identityStore.GetUserById(request.UserId);

        user.MarkMfaValidated(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.UpdateUser(user);

        return (authRequest, authResponse);
    }

    public async Task<AuthorizationResponse> ResendMfaCode(MfaRequest request)
    {
        _logger.LogDebug("Pre Authorization CorrelationId: {CorrelationId}", request.CorrelationId);

        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId}";

            _logger.LogWarning(message);

            return AuthorizationResponse.Failure(message);
        }

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        preAuthorization.UpdateMfaCode(request.UserId, mfaCode, DateTime.UtcNow.AddMinutes(5));

        await _preAuthorizationRepo.UpdatePreAuthorization(preAuthorization);

        _logger.LogInfo("Resend mfa code for user {UserId}", request.UserId);

        return await CompleteMfaProcess(request.UserId, mfaCode, request.CorrelationId);
    }

    private async Task<AuthorizationResponse> CompleteMfaProcess(int userId,
        string mfaCode,
        string correlationId)
    {
        var user = await _identityStore.GetUserById(userId);

        await SendNotification(
            user.TenantId,
            user.FullName,
            user.Email ?? string.Empty,
            mfaCode);

        user.MarkMfaChallengeSent(
            _currentUserService.CorrelationId,
            _currentUserService.IpAddress);

        await _identityStore.UpdateUser(user);

        _logger.LogInfo(
            "MFA challenge generated. CorrelationId: {CorrelationId}",
            correlationId);

        return AuthorizationResponse.Success(
            userId,
            correlationId,
            twoFactorEnabled: true);
    }

    private async Task SendNotification(int tenantId,
        string fullName,
        string email,
        string mfaCode)
    {
        var tokens = new Dictionary<string, string>
        {
            { "<%NAME%>", fullName},
            { "<%MFA_CODE%>",  mfaCode}
        };

        var modelJson = JsonSerializer.Serialize(tokens);

        var emailMessage = EmailMessage.CreateTemplate(
            tenantId: tenantId,
            messageKey: $"mfa:{tenantId}:{fullName}:{mfaCode}",
            new Domain.AggregateRoots.Emails.ValueObjects.EmailRecipient(new EmailAddress(email!), fullName),
            template: new EmailTemplateRef("MFA_CODE", modelJson),
            priority: 3,
            maxAttempts: 10,
            scheduledAtUtc: DateTime.UtcNow,
            correlationId: _currentUserService.CorrelationId,
            tags: "mfa-code"
        );

        await _emailQueueStore.EnqueueAsync(emailMessage, CancellationToken.None);
    }
}
