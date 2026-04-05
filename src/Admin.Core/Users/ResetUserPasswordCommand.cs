using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Users;

internal sealed class InitiateSelfServicePasswordResetCommand
{
    public string Email { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
}

internal sealed class InitiateAdminPasswordResetCommand
{
    public int UserId { get; init; }
}

internal sealed class CompletePasswordResetCommand
{
    public string RawToken { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

internal sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
}

internal sealed class CompletePasswordResetRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    public string NewPassword { get; init; } = string.Empty;
}
