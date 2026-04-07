using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Domain.AggregateRoots.Emails.ValueObjects;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Users.UseCases;

public sealed class EmailConfirmationUseCase
{
    private const int DefaultExpiryHours = 24;
    private const string InvalidOrExpiredTokenMessage = "Invalid or expired confirmation token.";

    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<EmailConfirmationUseCase> _logger;
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;

    public EmailConfirmationUseCase(
        ICurrentUserService currentUserService,
        IAppLogger<EmailConfirmationUseCase> logger,
        IEmailQueueRepository emailQueueRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _emailQueueRepository = emailQueueRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task InitiateEmailConfirmation(
        InitiateEmailConfirmationCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            throw new ArgumentException("UserId must be greater than zero.", nameof(request.UserId));
        }

        var user = await _userRepository.GetUserById(request.UserId);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInfo(
                "Email confirmation skipped for already confirmed user {UserId} in tenant {TenantId}",
                user.Id,
                user.TenantId);
            return;
        }

        var tokenData = GenerateTokenData();
        var confirmationToken = EmailConfirmationToken.Create(
            tenantId: user.TenantId,
            userId: user.Id,
            tokenHash: tokenData.TokenHash,
            expiresAt: tokenData.ExpiresAtUtc);

        confirmationToken.SetCreated(0);
        await _userRepository.CreateEmailConfirmationAsync(confirmationToken, cancellationToken);

        await EnqueueConfirmationEmail(
            user,
            confirmationToken.Id,
            tokenData.RawToken,
            request.AuthorizationContextId,
            DefaultExpiryHours,
            cancellationToken);

        _logger.LogInfo(
            "Email confirmation queued for user {UserId} in tenant {TenantId}",
            user.Id,
            user.TenantId);
    }

    public async Task<ApiResult<string>> CompleteEmailConfirmation(
        CompleteEmailConfirmationCommand request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken))
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("email.confirmation.invalid", InvalidOrExpiredTokenMessage));
        }

        var tokenHash = ComputeSha256(request.RawToken.Trim());
        var nowUtc = DateTime.UtcNow;

        var confirmationToken = await _userRepository.GetValidEmailConfirmationTokenAsync(
            tokenHash,
            nowUtc,
            cancellationToken);

        if (confirmationToken is null)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("email.confirmation.invalid", InvalidOrExpiredTokenMessage));
        }

        var user = await _userRepository.GetByTenantAsync(
            confirmationToken.UserId,
            confirmationToken.TenantId,
            cancellationToken);

        if (user is null)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("email.confirmation.invalid", InvalidOrExpiredTokenMessage));
        }

        if (!user.EmailConfirmed)
        {
            user.ConfirmEmail();
        }

        confirmationToken.MarkUsed();
        await _userRepository.UpdateUser(user);

        _logger.LogInfo(
            "Email confirmed for user {UserId} in tenant {TenantId}",
            user.Id,
            user.TenantId);

        return ApiResult<string>.Success("Email confirmed.");
    }

    private static (string RawToken, byte[] TokenHash, DateTime ExpiresAtUtc) GenerateTokenData()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = WebEncoders.Base64UrlEncode(randomBytes);
        var hash = ComputeSha256(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(DefaultExpiryHours);

        return (rawToken, hash, expiresAt);
    }

    private static byte[] ComputeSha256(string value)
        => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private async Task EnqueueConfirmationEmail(
        User user,
        long tokenId,
        string rawToken,
        string authorizationContextId,
        int expiryHours,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetSummaryAsync(user.TenantId, cancellationToken);

        var tenantName = tenant?.TenantDisplayName ?? tenant?.TenantName ?? "Tenant";
        var confirmLink = QueryHelpers.AddQueryString(
            $"{_currentUserService.BaseUrl}/confirm-email",
            BuildQuery(rawToken, authorizationContextId));

        var model = new Dictionary<string, string>
        {
            ["<%CONFIRM_LINK%>"] = confirmLink,
            ["<%EXPIRY_HOURS%>"] = expiryHours.ToString(),
            ["<%TENANT_NAME%>"] = tenantName
        };

        var message = EmailMessage.CreateTemplate(
            tenantId: user.TenantId,
            messageKey: $"email-confirmation:{tokenId}",
            recipient: new EmailRecipient(new EmailAddress(user.Email), user.FirstName),
            template: new EmailTemplateRef("EMAIL_CONFIRMATION", JsonSerializer.Serialize(model)),
            correlationId: _currentUserService.CorrelationId,
            tags: "email-confirmation");

        await _emailQueueRepository.EnqueueAsync(message, cancellationToken);
    }

    private static Dictionary<string, string?> BuildQuery(string rawToken, string authorizationContextId)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["token"] = rawToken
        };

        if (!string.IsNullOrWhiteSpace(authorizationContextId))
        {
            query["ctx"] = authorizationContextId;
        }

        return query;
    }
}


