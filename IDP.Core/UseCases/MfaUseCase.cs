using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Emails;
using IDP.Domain.AggregateRoots.Emails.ValueObjects;
using IDP.Foundation.Abstractions.Stores;
using System.Text.Json;

namespace IDP.Core.UseCases;

internal sealed class MfaUseCase : IMfaUseCase
{
    private readonly IIdentityStore _identityStore;
    private readonly IEmailQueueStore _emailQueueStore;
    private readonly IPreAuthorizationStore _preAuthorizationRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<MfaUseCase> _logger;

    public MfaUseCase(IIdentityStore identityStore,
        IPreAuthorizationStore preAuthorizationRepo,
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

    public async Task<AuthorizationResponse> GenerateMfaCode(AuthorizationRequest request, int userId)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogDebug("Pre Authorization CorrelationId: {CorrelationId}", correlationId);

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        PreAuthorization preAuthorization = new(userId,
            mfaCode,
            correlationId,
            request.ClientId,
            request.RedirectUri,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            DateTime.UtcNow.AddMinutes(5),
            request.Scopes);

        await _preAuthorizationRepo.Create(preAuthorization);

        _logger.LogInfo("Saved authorization code for user {UserId} (Client: {ClientId})",
            userId, request.ClientId);

        var user = await _identityStore.GetUserById(userId);

        await SendNotification(user.TenantId, user.FullName, user.Email ?? string.Empty, mfaCode);

        user.MarkMfaChallengeSent(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.UpdateUser(user);

        return AuthorizationResponse.Success(userId, correlationId, true);
    }

    public async Task<(AuthorizationRequest?, AuthorizationResponse)> VerifyMfaRequest(MfaRequest request)
    {
        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId} and Code:{request.Code}";
            _logger.LogWarning(message);
            return (default, AuthorizationResponse.Failure(message));
        }

        var authResponse = AuthorizationResponse.Success(preAuthorization.UserId, preAuthorization.CorrelationId, true);

        var authRequest = AuthorizationRequest.Create(preAuthorization.ClientId,
            preAuthorization.RedirectUri,
            preAuthorization.CodeChallenge,
            preAuthorization.CodeChallengeMethod,
            preAuthorization.Scopes);

        var user = await _identityStore.GetUserById(request.UserId);

        user.MarkMfaValidated(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.UpdateUser(user);

        return (authRequest, authResponse);
    }

    public async Task<AuthorizationResponse> ResendMfaCode(MfaRequest request)
    {
        _logger.LogDebug("Pre Authorization CorrelationId: {CorrelationId}", request.CorrelationId);

        var preAuthorization = await _preAuthorizationRepo
            .GetPreAuthorization(request.CorrelationId, request.UserId);

        if (preAuthorization == null)
        {
            var message = $"Mfa code not found or expired for UserId: {request.UserId}";

            _logger.LogWarning(message);

            return AuthorizationResponse.Failure(message);
        }

        var mfaCode = MfaCodeGenerator.GenerateMfaCode();

        preAuthorization.UpdateMfaCode(request.UserId, mfaCode, DateTime.UtcNow.AddMinutes(5));

        await _preAuthorizationRepo.Update(preAuthorization);

        _logger.LogInfo("Resend mfa code for user {UserId}", request.UserId);

        var user = await _identityStore.GetUserById(request.UserId);

        await SendNotification(user.TenantId, user.FullName, user.Email ?? string.Empty, mfaCode);

        user.MarkMfaChallengeSent(_currentUserService.CorrelationId, _currentUserService.IpAddress);

        await _identityStore.UpdateUser(user);

        return AuthorizationResponse.Success(request.UserId, request.CorrelationId, true);
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
            recipients: new[]
            {
                new Domain.AggregateRoots.Emails.ValueObjects.EmailRecipient(RecipientType.To, new EmailAddress(email!), fullName)
            },
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
