namespace TokenIDP.Domain.AggregateRoots.Clients;

public class ClientValidationSnapshot
{
    private readonly IReadOnlyDictionary<string, string> _scopeResourceMap;

    public string ClientId { get; }
    public string ClientName { get; }
    public int TenantId { get; }
    public bool IsActive { get; }
    public string RedirectUri { get; } = string.Empty;
    public string? LogoutRedirectUri { get; }
    public ClientTypes ClientType { get; }
    public TokenTypes TokenType { get; }
    public IReadOnlySet<GrantTypes> GrantTypes { get; }
    public IReadOnlySet<string> Scopes { get; }
    public IReadOnlySet<string> ApiResources { get; }
    public IReadOnlySet<string> ActiveSecretHashes { get; }
    public int? ClientSecretExpiry { get; }
    public int AccessTokenLifetime { get; }
    public int AuthorizationCodeLifetime { get; }
    public int RefreshTokenExpiration { get; }
    public bool CibaEnabled { get; }
    public CibaTokenDeliveryModes BackchannelTokenDeliveryMode { get; }
    public int CibaDefaultExpirySeconds { get; }
    public int CibaMinIntervalSeconds { get; }
    public bool RequireCibaUserCode { get; }
    public bool AllowCibaLoginHint { get; }
    public bool AllowCibaLoginHintToken { get; }
    public bool AllowCibaIdTokenHint { get; }

    public ClientValidationSnapshot(
        string clientId,
        string clientName,
        int tenantId,
        bool isActive,
        string redirectUri,
        string? logoutRedirectUri,
        ClientTypes clientType,
        TokenTypes tokenType,
        IEnumerable<GrantTypes> grantTypes,
        IEnumerable<string> scopes,
        IEnumerable<string> apiResources,
        IEnumerable<ClientApiScopeAssignment> apiScopeAssignments,
        IEnumerable<string> activeSecretHashes,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        int? clientSecretExpiry,
        bool cibaEnabled,
        CibaTokenDeliveryModes backchannelTokenDeliveryMode,
        int cibaDefaultExpirySeconds,
        int cibaMinIntervalSeconds,
        bool requireCibaUserCode,
        bool allowCibaLoginHint,
        bool allowCibaLoginHintToken,
        bool allowCibaIdTokenHint)
    {
        ClientId = clientId;
        ClientName = clientName;
        TenantId = tenantId;
        IsActive = isActive;
        RedirectUri = redirectUri;
        LogoutRedirectUri = logoutRedirectUri;
        ClientType = clientType;
        TokenType = tokenType;
        GrantTypes = grantTypes.ToHashSet();
        Scopes = scopes.ToHashSet();
        ApiResources = apiResources.ToHashSet();
        _scopeResourceMap = apiScopeAssignments.ToDictionary(
            x => x.ScopeName,
            x => x.ApiResourceName,
            StringComparer.Ordinal);
        ActiveSecretHashes = activeSecretHashes.ToHashSet();
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        ClientSecretExpiry = clientSecretExpiry;
        CibaEnabled = cibaEnabled;
        BackchannelTokenDeliveryMode = backchannelTokenDeliveryMode;
        CibaDefaultExpirySeconds = cibaDefaultExpirySeconds;
        CibaMinIntervalSeconds = cibaMinIntervalSeconds;
        RequireCibaUserCode = requireCibaUserCode;
        AllowCibaLoginHint = allowCibaLoginHint;
        AllowCibaLoginHintToken = allowCibaLoginHintToken;
        AllowCibaIdTokenHint = allowCibaIdTokenHint;
    }

    public bool TryGetApiResourceForScope(string scopeName, out string apiResourceName)
    {
        return _scopeResourceMap.TryGetValue(scopeName, out apiResourceName!);
    }
}
