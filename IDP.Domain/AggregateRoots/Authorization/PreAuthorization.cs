namespace IDP.Domain.AggregateRoots.Authorization;

public class PreAuthorization : AuditableAggregate<int>
{
    public int UserId { get; private set; }
    public string CorrelationId { get; private set; }
    public string ClientId { get; private set; }
    public string RedirectUri { get; private set; }
    public string CodeChallenge { get; private set; }
    public string CodeChallengeMethod { get; private set; } //Default is SHA-256
    public string Scopes { get; private set; }
    public string MfaCode { get; private set; }
    public DateTime Expiry { get; private set; }
    public bool Is2FAVerified { get; private set; }

    private PreAuthorization() { }

    public PreAuthorization(int userId,
        string mfaCode,
        string correlationId,
        string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        DateTime expiry,
        string scopes = null)
    {
        UserId = userId;
        MfaCode = mfaCode;
        CorrelationId = correlationId;
        ClientId = clientId;
        RedirectUri = redirectUri;
        CodeChallenge = codeChallenge;
        Scopes = scopes;
        CodeChallengeMethod = codeChallengeMethod;
        Expiry = expiry;
        SetCreated(userId);
    }

    public void UpdateMfaCode(int userId,
        string mfaCode,
        DateTime expiry)
    {
        MfaCode = mfaCode;
        Expiry = expiry;
        SetUpdated(userId);
    }

    public void UpdateTwoFactorEnableFlag(bool enabled)
    {
        Is2FAVerified = enabled;
    }
}
