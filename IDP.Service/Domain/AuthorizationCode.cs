namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class AuthorizationCode
{
    [Key]
    public int Id { get; private set; }
    public string Code { get; private set; }
    public string ClientId { get; private set; }
    public int UserId { get; private set; }
    public DateTime Expiry { get; private set; }
    public string RedirectUri { get; private set; }
    public string CodeChallenge { get; private set; }
    public string CodeChallengeMethod { get; private set; } //Default is SHA-256
    public string Scopes { get; private set; }
    public bool IsUsed { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public virtual User User { get; private set; }

    private AuthorizationCode() { }

    public AuthorizationCode(string code,
        string codeChallenge,
        string codeChallengeMethod,
        string clientId,
        int userId,
        DateTime expiry,
        string redirectUri,
        string scopes = null)
    {
        CodeChallenge = codeChallenge;
        Code = code;
        ClientId = clientId;
        UserId = userId;
        Expiry = expiry;
        RedirectUri = redirectUri;
        CreatedBy = userId;
        CreatedOn = DateTime.UtcNow;
        Scopes = scopes;
        CodeChallengeMethod = codeChallengeMethod;
    }

    public void UpdateIsUsed(bool isUsed, int userId)
    {
        IsUsed = isUsed;
        UpdatedBy = userId;
        UpdatedOn = DateTime.UtcNow;
    }
}
