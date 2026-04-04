using Admin.Core.Common;
using IDP.Domain.AggregateRoots.Emails;
using IDP.Domain.AggregateRoots.Emails.ValueObjects;
using IDP.Domain.AggregateRoots.Tokens;
using IDP.Foundation.Abstractions.Stores;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Admin.Core.Users.UseCases;

internal sealed class PasswordResetUseCase
{
    private const int DefaultExpiryMinutes = 30;
    private const string GenericForgotPasswordMessage =
        "If the account exists, a password reset link will be sent.";
    private const string InvalidOrExpiredTokenMessage = "Invalid or expired reset token.";

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<PasswordResetUseCase> _logger;
    private readonly PasswordService _passwordService;
    private readonly IEmailQueueStore _emailQueueStore;
    private readonly IClientStore _clientStore;

    public PasswordResetUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<PasswordResetUseCase> logger,
        PasswordService passwordService,
        IEmailQueueStore emailQueueStore,
        IClientStore clientStore)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _passwordService = passwordService;
        _emailQueueStore = emailQueueStore;
        _clientStore = clientStore;
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

        var client = await _clientStore.GetClientShortInfo(request.ClientId);

        if (client == null || !client.AllowForgotPassword)
        {
            return ApiResult<string>.Success(GenericForgotPasswordMessage);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TenantId == client.TenantId && u.Email == request.Email,
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

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
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

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

        var resetToken = await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > nowUtc,
                cancellationToken);

        if (resetToken == null)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("password.reset.invalid", InvalidOrExpiredTokenMessage));
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == resetToken.UserId && u.TenantId == resetToken.TenantId,
                cancellationToken);

        if (user == null)
        {
            return ApiResult<string>.Failure(
                ApiError.Failure("password.reset.invalid", InvalidOrExpiredTokenMessage));
        }

        _passwordService.SetPassword(user, request.NewPassword.Trim());
        user.MarkPasswordResetCompleted(_currentUserService.CorrelationId);
        resetToken.MarkUsed();

        var tokens = await _dbContext.Tokens
            .Where(t => t.UserId == user.Id && t.TokenStatus != TokenStatus.Revoked)
            .ToListAsync(cancellationToken);

        var revokedByUserId = _currentUserService.UserId > 0
            ? _currentUserService.UserId
            : user.Id;

        foreach (var token in tokens)
        {
            token.Revoke("PasswordReset", "system", revokedByUserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo(
            "Password reset completed for user {UserId} in tenant {TenantId}. Revoked tokens: {TokenCount}",
            user.Id,
            user.TenantId,
            tokens.Count);

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
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        var tenantName = tenant?.TenantDisplayName ?? tenant?.TenantName ?? "Tenant";
        var tenantKey = tenant?.TenantKey ?? "app";
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

        await _emailQueueStore.EnqueueAsync(message, cancellationToken);
    }
}