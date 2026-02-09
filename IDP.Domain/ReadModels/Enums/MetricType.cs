namespace IDP.Domain.ReadModels.Enums;

public static class MetricType
{
    public const string TokensIssued = "tokens_issued";
    public const string TokensIssuedPerClient = "tokens_issued_per_client";
    public const string RefreshTokensIssued = "refresh_tokens_issued";
    public const string TokensIssuedPerGrant = "tokens_issued_per_grant";
    public const string AuthSuccess = "auth_success";
    public const string AuthFailed = "auth_failed";
    public const string MfaChallenges = "mfa_challenges";
    public const string AccountLockout = "account_lockout";
    public const string MultipleFailedAttempts = "multiple_failed_attempts";
}
