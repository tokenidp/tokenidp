namespace TokenIDP.Domain.AggregateRoots.Authorization;

public class PreAuthorization : AggregateRoot<int>
{
    public int? UserId { get; private set; }
    public int TenantId { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public int ClientId_FK { get; private set; }
    public string? ClientId { get; private set; } = default!;
    public string? State { get; private set; } = default!;
    public string? RedirectUri { get; private set; } = default!;
    public string? CodeChallenge { get; private set; } = default!;
    public string? CodeChallengeMethod { get; private set; } = default!;//Default is SHA-256 
    public string? GrantType { get; private set; } = default!;
    public string? Scopes { get; private set; } = default!;
    public string? MfaCode { get; private set; } = default!;
    public DateTime Expiry { get; private set; }
    public bool? Is2FAVerified { get; private set; }

    private PreAuthorization() { }

    public PreAuthorization(int tenantId,
        string correlationId,
        int clientid_fk,
        string? clientId,
        string? redirectUri,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? grantType,
        string? state,
        string? scopes)
    {
        TenantId = tenantId;
        CorrelationId = correlationId;
        ClientId = clientId;
        RedirectUri = redirectUri;
        CodeChallenge = codeChallenge;
        Scopes = scopes;
        CodeChallengeMethod = codeChallengeMethod;
        GrantType = grantType;
        State = state;
        ClientId_FK = clientid_fk;
        Expiry = DateTime.UtcNow.AddMinutes(5);
    }

    public PreAuthorization(int tenantId,
        string correlationId,
        string? clientId,
        string mfaCode,
        string? scopes,
        DateTime expiry,
        int? userId = null)
    {
        TenantId = tenantId;
        CorrelationId = correlationId;
        ClientId = clientId;
        Scopes = scopes;
        MfaCode = mfaCode;
        Expiry = expiry;
        UserId = userId;
    }

    public void UpdateMfaCode(int userId,
        string mfaCode,
        DateTime expiry)
    {
        MfaCode = mfaCode;
        Expiry = expiry;
    }

    public void UpdateTwoFactorEnableFlag(bool enabled)
    {
        Is2FAVerified = enabled;
    }
}

