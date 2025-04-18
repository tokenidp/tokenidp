namespace IDP.Service.Domain;

public class PreAuthorization
{
    [Key]
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string CorrelationId { get; private set; }
    public string ClientId { get; private set; }
    public string RedirectUri { get; private set; }
    public string CodeChallenge { get; private set; }
    public string CodeChallengeMethod { get; private set; } //Default is SHA-256
    public string Scopes { get; private set; }
    public string MfaCode {  get; private set; }
    public DateTime Expiry { get; private set; }
    public bool Is2FAVerified { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

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
    }

    public void UpdateTwoFactorEnableFlag(bool enabled)
    {
        Is2FAVerified = enabled;
    }
}
