namespace IDP.Domain.ReadModels.Enums;

public enum ActivityEventType
{
    // Authentication (1000–1999)
    LoginSucceeded = 1000,
    LoginFailed = 1001,
    Logout = 1002,
    MfaChallengeSent = 1003,
    MfaValidated = 1004,
    MfaFailed = 1005,
    PasswordResetRequested = 1006,
    PasswordResetCompleted = 1007,
    AccountLocked = 1008,
    AccountUnlocked = 1009,

    // Authorization & Tokens (2000–2999)
    TokenJWTIssued = 2000,
    TokenRefreshIssue = 2001,
    TokenRevoked = 2002,
    TokenExpired = 2003,
    ScopeDenied = 2004,
    ConsentGranted = 2005,
    ConsentRevoked = 2006,
    TokenReferenceIssue = 2007,

    // User Management (3000–3999)
    UserCreated = 3000,
    UserUpdated = 3001,
    UserDeleted = 3002,
    UserDisabled = 3003,
    UserEnabled = 3004,
    RoleAssigned = 3005,
    RoleRemoved = 3006,
    PermissionGranted = 3007,
    PermissionRevoked = 3008,

    // Client Management (4000–4999)
    ClientCreated = 4000,
    ClientUpdated = 4001,
    ClientDisabled = 4002,
    ClientEnabled = 4003,
    ClientSecretRotated = 4004,
    GrantTypeChanged = 4005,

    // Tenant Management (5000–5999)
    TenantCreated = 5000,
    TenantUpdated = 5001,
    TenantDisabled = 5002,
    SecurityPolicyChanged = 5003,
    MfaPolicyChanged = 5004,
    IpRestrictionChanged = 5005,

    // System & Security (6000–6999)
    SuspiciousLoginDetected = 6000,
    BruteForceDetected = 6001,
    RateLimitTriggered = 6002,
    OutboxRetry = 6003,
    OutboxFailed = 6004,
    BackgroundJobFailed = 6005,
    SigningKeyRotated = 6006,
    CertificateExpired = 6007
}