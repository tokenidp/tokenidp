using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Users;

public sealed class InitiateEmailConfirmationCommand
{
    public int UserId { get; init; }
    public string AuthorizationContextId { get; init; } = string.Empty;
}

public sealed class CompleteEmailConfirmationCommand
{
    public string RawToken { get; init; } = string.Empty;
}

public sealed class CompleteEmailConfirmationRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;
}