namespace IDP.Domain.Specifications;

public static class OutboxEventTypes
{
    public const string TokenIssued = "token.issued";
    public const string RefreshTokenIssued = "token.refresh_issued";
    public const string ReferenceTokenIssued = "token.reference_issued";
    public const string TokenRevoked = "token.revoked";
    public const string TokenExpired = "token.expired";
    public const string TokenRefreshRotated = "token.refresh_rotated";
    public const string TokenRefreshReuseDetected = "token.refresh_reuse_detected";
}
