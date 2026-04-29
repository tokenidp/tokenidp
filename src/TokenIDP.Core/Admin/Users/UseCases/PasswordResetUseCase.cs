using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Domain.AggregateRoots.Emails.ValueObjects;

namespace TokenIDP.Core.Admin.Users.UseCases;

internal sealed class PasswordResetUseCase
{
    private const int DefaultExpiryMinutes = 30;
    private const string GenericForgotPasswordMessage =
        "If the account exists, a password reset link will be sent.";
    private const string InvalidOrExpiredTokenMessage = "Invalid or expired reset token.";

    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<PasswordResetUseCase> _logger;
    private readonly PasswordService _passwordService;
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly ITenantRepository _tenantRepository;

    public PasswordResetUseCase(
        ICurrentUserService currentUserService,
        IAppLogger<PasswordResetUseCase> logger,
        PasswordService passwordService,
        IEmailQueueRepository emailQueueRepository,
        IClientRepository clientRepository,
        IUserRepository userRepository,
        ITokenRepository tokenRepository,
        ITenantRepository tenantRepository)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _passwordService = passwordService;
        _emailQueueRepository = emailQueueRepository;
        _clientRepository = clientRepository;
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<ApiResult<string>> InitiateSelfServicePasswordReset(
        InitiateSelfServicePasswordResetCommand request,
        CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ApiResult<string>.Success(GenericForgotPasswordMessage);
        }

        var client = await _clientRepository.GetClientShortInfo(request.ClientId);

        if (client == null || !client.AllowForgotPassword)
        {
            return ApiResult<string>.Success(GenericForgotPasswordMessage);
        }

        var user = await _userRepository.GetByTenantAndEmailAsync(
            client.TenantId,
            request.Email,
            cancellationToken);

        if (user == null)
        {
            return ApiResult<string>.Success(GenericForgotPasswordMessage);
        }

        var tokenData = GenerateTokenData();

        var resetToken = PasswordResetToken.Create(
            tenantId: user.TenantId,
            userId: user.Id,
            tokenHash: tokenData.TokenHash,
            expiresAt: tokenData.ExpiresAtUtc,
            requestedByType: PasswordResetRequestedByType.SelfService);

        resetToken.SetCreated(0);
        user.MarkPasswordResetRequested(_currentUserService.CorrelationId);

        await _userRepository.CreatePasswordResetAsync(user, resetToken, cancellationToken);

        await EnqueueResetEmail(user, resetToken.Id, tokenData.RawToken, DefaultExpiryMinutes, cancellationToken);

        _logger.LogInfo(
            "Self-service password reset requested for user {UserId} in tenant {TenantId}",
            user.Id,
            user.TenantId);

        return ApiResult<string>.Success(GenericForgotPasswordMessage);
    }

    public async Task<ApiResult<string>> InitiateAdminPasswordReset(
        InitiateAdminPasswordResetCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("user.id.invalid", "User Id should be greater than zero."));
        }

        var tenantId = _currentUserService.TenantId;

        var user = await _userRepository.GetByTenantAsync(
            request.UserId,
            tenantId,
            cancellationToken);

        if (user == null)
        {
            _logger.LogWarning(
                "Admin password reset denied for user {UserId} in tenant {TenantId}: user not found in current tenant.",
                request.UserId,
                tenantId);

            return ApiResult<string>.Failure(
                ApiError.Failure("NotFound", "User not found."));
        }

        var tokenData = GenerateTokenData();

        var resetToken = PasswordResetToken.Create(
            tenantId: user.TenantId,
            userId: user.Id,
            tokenHash: tokenData.TokenHash,
            expiresAt: tokenData.ExpiresAtUtc,
            requestedByType: PasswordResetRequestedByType.Admin);

        resetToken.SetCreated(_currentUserService.UserId > 0 ? _currentUserService.UserId : 0);
        user.MarkPasswordResetRequested(_currentUserService.CorrelationId);

        await _userRepository.CreatePasswordResetAsync(user, resetToken, cancellationToken);

        await EnqueueResetEmail(user, resetToken.Id, tokenData.RawToken, DefaultExpiryMinutes, cancellationToken);

        _logger.LogInfo(
            "Admin initiated password reset for user {UserId} in tenant {TenantId}",
            user.Id,
            user.TenantId);

        return ApiResult<string>.Success("Password reset email queued.");
    }

    public async Task<ApiResult<string>> CompletePasswordReset(
        CompletePasswordResetCommand request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken))
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("password.reset.invalid", InvalidOrExpiredTokenMessage));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("user.password.required", "Password is required."));
        }

        var tokenHash = ComputeSha256(request.RawToken.Trim());
        var nowUtc = DateTime.UtcNow;

        var resetToken = await _userRepository.GetValidPasswordResetTokenAsync(
            tokenHash,
            nowUtc,
            cancellationToken);

        if (resetToken == null)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("password.reset.invalid", InvalidOrExpiredTokenMessage));
        }

        var user = await _userRepository.GetByTenantAsync(
            resetToken.UserId,
            resetToken.TenantId,
            cancellationToken);

        if (user == null)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("password.reset.invalid", InvalidOrExpiredTokenMessage));
        }

        _passwordService.SetPassword(user, request.NewPassword.Trim());
        user.MarkPasswordResetCompleted(_currentUserService.CorrelationId);
        resetToken.MarkUsed();

        var revokedByUserId = _currentUserService.UserId > 0
            ? _currentUserService.UserId
            : user.Id;
        var revokedCount = await _tokenRepository.RevokeActiveTokensForUserAsync(
            user.TenantId,
            user.Id,
            "PasswordReset",
            "system",
            revokedByUserId,
            cancellationToken);

        _logger.LogInfo(
            "Password reset completed for user {UserId} in tenant {TenantId}. Revoked tokens: {TokenCount}",
            user.Id,
            user.TenantId,
            revokedCount);

        return ApiResult<string>.Success("Password reset completed.");
    }

    private static (string RawToken, byte[] TokenHash, DateTime ExpiresAtUtc) GenerateTokenData()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = WebEncoders.Base64UrlEncode(randomBytes);
        var hash = ComputeSha256(rawToken);
        var expiresAt = DateTime.UtcNow.AddMinutes(DefaultExpiryMinutes);

        return (rawToken, hash, expiresAt);
    }

    private static byte[] ComputeSha256(string value)
        => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private async Task EnqueueResetEmail(
        User user,
        long tokenId,
        string rawToken,
        int expiryMinutes,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetSummaryAsync(user.TenantId, cancellationToken);

        var tenantName = tenant?.TenantDisplayName ?? tenant?.TenantName ?? "Tenant";
        var resetLink =
            $"{_currentUserService.BaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        var model = new Dictionary<string, string>
        {
            ["<%RESET_LINK%>"] = resetLink,
            ["<%EXPIRY_MINUTES%>"] = expiryMinutes.ToString(),
            ["<%TENANT_NAME%>"] = tenantName
        };

        var message = EmailMessage.CreateTemplate(
            tenantId: user.TenantId,
            messageKey: $"password-reset:{tokenId}",
            recipient: new EmailRecipient(new EmailAddress(user.Email), user.FirstName),
            template: new EmailTemplateRef("PASSWORD_RESET", JsonSerializer.Serialize(model)),
            correlationId: _currentUserService.CorrelationId,
            tags: "password-reset");

        await _emailQueueRepository.EnqueueAsync(message, cancellationToken);
    }
}


