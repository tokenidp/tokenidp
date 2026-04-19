namespace TokenIDP.Domain.AggregateRoots.Clients;

public enum ClientTypes
{
    SPA,
    Mobile,
    Desktop,
    WebApp,
    Backend,
    DeviceIoT
}

public enum TokenTypes
{
    JWT,
    ReferenceToken
}

public enum CibaTokenDeliveryModes
{
    Poll,
    Ping,
    Push
}

public enum RefreshTokenDeliveryMode
{
    Response = 1,
    Cookie = 2,
    Both = 3
}

public class Client : AggregateRoot<int>, ITenant
{
    public string ClientId { get; private set; } = default!;
    public string ClientName { get; private set; } = default!;
    public string? Description { get; private set; }
    public ClientTypes ClientType { get; private set; }
    public TokenTypes TokenType { get; private set; }
    public int TenantId { get; private set; }
    public string RedirectUri { get; private set; } = default!;
    public string? LogoutRedirectUri { get; private set; }
    public bool RequiredPkce { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public int? ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int AuthorizationCodeLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public RefreshTokenDeliveryMode RefreshTokenDeliveryMode { get; private set; }
    public int? PermitLimit { get; private set; }
    public TimeSpan? TimeWindow { get; private set; }
    public int? QueueLimit { get; private set; }
    public bool? EnableITracking { get; private set; }
    public bool CibaEnabled { get; private set; }
    public CibaTokenDeliveryModes BackchannelTokenDeliveryMode { get; private set; }
    public int CibaDefaultExpirySeconds { get; private set; }
    public int CibaMinIntervalSeconds { get; private set; }
    public bool RequireCibaUserCode { get; private set; }
    public bool AllowCibaLoginHint { get; private set; }
    public bool AllowCibaLoginHintToken { get; private set; }
    public bool AllowCibaIdTokenHint { get; private set; }

    public virtual Tenant Tenant { get; private set; } = default!;
    public virtual ClientAuthPolicy ClientAuthPolicy { get; private set; } = default!;
    public virtual ICollection<ClientExternalProvider> ClientExternalProviders { get; private set; } = default!;
    public virtual ICollection<ClientScope> ClientScopes { get; private set; } = default!;
    public virtual ICollection<ClientGrantType> ClientGrantTypes { get; private set; } = default!;
    public virtual ICollection<ClientSecret> ClientSecrets { get; private set; } = default!;
    public virtual ICollection<ClientApiResource> ClientApiResources { get; private set; } = default!;

    private Client()
    {

    }

    private Client(int tenantId,
        string clientId,
        string clientName,
        string? description,
        ClientTypes appType,
        TokenTypes tokenType,
        string redirectUri,
        string? logoutRedirectUri,
        bool isActive,
        int? clientSecretExpiry,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        RefreshTokenDeliveryMode refreshTokenDeliveryMode,
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking,
        bool cibaEnabled,
        CibaTokenDeliveryModes backchannelTokenDeliveryMode,
        int cibaDefaultExpirySeconds,
        int cibaMinIntervalSeconds,
        bool requireCibaUserCode,
        bool allowCibaLoginHint,
        bool allowCibaLoginHintToken,
        bool allowCibaIdTokenHint)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ClientName = clientName;
        Description = description;
        ClientType = appType;
        TokenType = tokenType;
        RedirectUri = redirectUri;
        LogoutRedirectUri = logoutRedirectUri;
        IsActive = isActive;
        IsDeleted = false;
        ClientSecretExpiry = clientSecretExpiry;
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        RefreshTokenDeliveryMode = refreshTokenDeliveryMode;
        PermitLimit = permitLimit;
        TimeWindow = timeWindow;
        QueueLimit = queueLimit;
        EnableITracking = enableITracking;
        CibaEnabled = cibaEnabled;
        BackchannelTokenDeliveryMode = backchannelTokenDeliveryMode;
        CibaDefaultExpirySeconds = cibaDefaultExpirySeconds;
        CibaMinIntervalSeconds = cibaMinIntervalSeconds;
        RequireCibaUserCode = requireCibaUserCode;
        AllowCibaLoginHint = allowCibaLoginHint;
        AllowCibaLoginHintToken = allowCibaLoginHintToken;
        AllowCibaIdTokenHint = allowCibaIdTokenHint;

        ClientScopes = new List<ClientScope>();
        ClientGrantTypes = new List<ClientGrantType>();
        ClientSecrets = new List<ClientSecret>();
        ClientApiResources = new List<ClientApiResource>();
        ClientExternalProviders = new List<ClientExternalProvider>();
    }

    public Result UpdateClient(
        string clientName,
        string? description,
        ClientTypes appType,
        TokenTypes tokenType,
        string redirectUri,
        string? logoutRedirectUri,
        bool isActive,
        int? clientSecretExpiry,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        RefreshTokenDeliveryMode refreshTokenDeliveryMode,
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking,
        bool cibaEnabled,
        CibaTokenDeliveryModes backchannelTokenDeliveryMode,
        int cibaDefaultExpirySeconds,
        int cibaMinIntervalSeconds,
        bool requireCibaUserCode,
        bool allowCibaLoginHint,
        bool allowCibaLoginHintToken,
        bool allowCibaIdTokenHint)
    {
        if (IsDeleted)
        {
            return Result.Failure("client.deleted", "Deleted client cannot be modified.");
        }

        var validation = ValidateInput(
            ClientId,
            clientName,
            redirectUri,
            accessTokenLifetime,
            authorizationCodeLifetime,
            refreshTokenExpiration,
            refreshTokenDeliveryMode);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (cibaEnabled)
        {
            var cibaValidation = ValidateCibaSettings(
                backchannelTokenDeliveryMode,
                cibaDefaultExpirySeconds,
                cibaMinIntervalSeconds,
                allowCibaLoginHint,
                allowCibaLoginHintToken,
                allowCibaIdTokenHint);
            if (!cibaValidation.IsSuccess)
            {
                return cibaValidation;
            }
        }

        ClientName = clientName;
        Description = description;
        ClientType = appType;
        TokenType = tokenType;
        RedirectUri = redirectUri;
        LogoutRedirectUri = logoutRedirectUri;
        IsActive = isActive;
        ClientSecretExpiry = clientSecretExpiry;
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        RefreshTokenDeliveryMode = refreshTokenDeliveryMode;
        PermitLimit = permitLimit;
        TimeWindow = timeWindow;
        QueueLimit = queueLimit;
        EnableITracking = enableITracking;
        CibaEnabled = cibaEnabled;
        BackchannelTokenDeliveryMode = backchannelTokenDeliveryMode;
        CibaDefaultExpirySeconds = cibaDefaultExpirySeconds;
        CibaMinIntervalSeconds = cibaMinIntervalSeconds;
        RequireCibaUserCode = requireCibaUserCode;
        AllowCibaLoginHint = allowCibaLoginHint;
        AllowCibaLoginHintToken = allowCibaLoginHintToken;
        AllowCibaIdTokenHint = allowCibaIdTokenHint;

        return Result.Success(Id);
    }

    public Result AddSecret(ClientSecret clientSecret)
    {
        if (clientSecret == null)
        {
            return Result.Failure("client.secret.invalid", "Client secret cannot be empty.");
        }

        if (ClientSecrets.Any(secret =>
                string.Equals(secret.SecretHash, clientSecret.SecretHash, StringComparison.Ordinal)))
        {
            return Result.Success(Id);
        }

        ClientSecrets.Add(clientSecret);
        return Result.Success(Id);
    }

    public Result SoftDelete()
    {
        if (IsDeleted)
        {
            return Result.Failure("client.deleted", "Client is already deleted.");
        }

        IsDeleted = true;
        IsActive = false;

        return Result.Success(Id);
    }

    public bool RequiresClientSecret()
    {
        return ClientType is ClientTypes.WebApp or ClientTypes.Backend;
    }

    public void RevokeActiveSecrets()
    {
        foreach (var secret in ClientSecrets.Where(s => !s.IsRevoked))
        {
            secret.Revoke();
        }
    }

    public Result ReplaceScopes(IEnumerable<ClientScope> scopes)
    {
        if (scopes == null)
        {
            return Result.Success(Id);
        }

        ClientScopes.Clear();
        foreach (var scope in scopes)
        {
            ClientScopes.Add(scope);
        }

        return Result.Success(Id);
    }

    public Result ReplaceGrantTypes(IEnumerable<ClientGrantType> grantTypes)
    {
        if (grantTypes == null)
        {
            return Result.Success(Id);
        }

        ClientGrantTypes.Clear();
        foreach (var grantType in grantTypes)
        {
            ClientGrantTypes.Add(grantType);
        }

        return Result.Success(Id);
    }

    public Result ReplaceApiResources(IEnumerable<ClientApiResource> apiResources)
    {
        if (apiResources == null)
        {
            return Result.Success(Id);
        }

        ClientApiResources.Clear();
        foreach (var apiResource in apiResources)
        {
            ClientApiResources.Add(apiResource);
        }

        return Result.Success(Id);
    }

    public Result ConfigureAuthPolicy(
        bool allowLocalLoginOverride,
        bool allowSelfRegistrationOverride,
        bool mfaPolicyOverride,
        bool showExternalProviders,
        bool showStaySignedIn,
        bool showCreateAccountLink,
        bool autoCreateUsers,
        int? defaultRoleId)
    {
        if (ClientAuthPolicy == null)
        {
            ClientAuthPolicy = ClientAuthPolicy.Create(
                this,
                allowLocalLoginOverride,
                allowSelfRegistrationOverride,
                mfaPolicyOverride,
                showExternalProviders,
                showStaySignedIn,
                showCreateAccountLink,
                autoCreateUsers,
                defaultRoleId);
        }
        else
        {
            ClientAuthPolicy.update(
                allowLocalLoginOverride,
                allowSelfRegistrationOverride,
                mfaPolicyOverride,
                showExternalProviders,
                showStaySignedIn,
                showCreateAccountLink,
                autoCreateUsers,
                defaultRoleId);
        }

        return Result.Success(Id);
    }

    public Result ReplaceExternalProviders(IEnumerable<int> externalProviderIds)
    {
        externalProviderIds ??= Array.Empty<int>();

        var sourceProviderIds = externalProviderIds.ToList();

        if (sourceProviderIds.Any(id => id <= 0))
        {
            return Result.Failure(
                "client.external_providers.invalid",
                "External providers contain invalid values.");
        }

        var providerIds = sourceProviderIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (providerIds.Count != sourceProviderIds.Count)
        {
            return Result.Failure(
                "client.external_providers.invalid",
                "External providers contain duplicate values.");
        }

        ClientExternalProviders.Clear();

        foreach (var providerId in providerIds)
        {
            ClientExternalProviders.Add(ClientExternalProvider.Create(providerId));
        }

        return Result.Success(Id);
    }
    public static Result Create(
        int tenantId,
        string clientId,
        string clientName,
        string? description,
        ClientTypes appType,
        TokenTypes tokenType,
        string redirectUri,
        string? logoutRedirectUri,
        bool isActive,
        int? clientSecretExpiry,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        RefreshTokenDeliveryMode refreshTokenDeliveryMode,
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking,
        bool cibaEnabled,
        CibaTokenDeliveryModes backchannelTokenDeliveryMode,
        int cibaDefaultExpirySeconds,
        int cibaMinIntervalSeconds,
        bool requireCibaUserCode,
        bool allowCibaLoginHint,
        bool allowCibaLoginHintToken,
        bool allowCibaIdTokenHint,
        out Client? client)
    {
        client = null;

        var validation = ValidateInput(
            clientId,
            clientName,
            redirectUri,
            accessTokenLifetime,
            authorizationCodeLifetime,
            refreshTokenExpiration,
            refreshTokenDeliveryMode);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (cibaEnabled)
        {
            var cibaValidation = ValidateCibaSettings(
                backchannelTokenDeliveryMode,
                cibaDefaultExpirySeconds,
                cibaMinIntervalSeconds,
                allowCibaLoginHint,
                allowCibaLoginHintToken,
                allowCibaIdTokenHint);
            if (!cibaValidation.IsSuccess)
            {
                return cibaValidation;
            }
        }

        client = new Client(
            tenantId,
            clientId.Trim(),
            clientName.Trim(),
            description?.Trim(),
            appType,
            tokenType,
            redirectUri.Trim(),
            logoutRedirectUri?.Trim(),
            isActive,
            clientSecretExpiry,
            accessTokenLifetime,
            authorizationCodeLifetime,
            refreshTokenExpiration,
            refreshTokenDeliveryMode,
            permitLimit,
            timeWindow,
            queueLimit,
            enableITracking,
            cibaEnabled,
            backchannelTokenDeliveryMode,
            cibaDefaultExpirySeconds,
            cibaMinIntervalSeconds,
            requireCibaUserCode,
            allowCibaLoginHint,
            allowCibaLoginHintToken,
            allowCibaIdTokenHint);

        return Result.Success(0);
    }

    private static Result ValidateInput(
        string clientId,
        string clientName,
        string redirectUri,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration,
        RefreshTokenDeliveryMode refreshTokenDeliveryMode)
    {
        var validation = ValidateRequired(clientId, "client.id.invalid",
                "Client Id cannot be empty.")
            .Combine(ValidateRequired(clientName, "client.name.invalid",
                "Client name cannot be empty."))
            .Combine(ValidateOptionalAbsoluteUri(redirectUri, "client.redirect.invalid",
                "Redirect URI must be a valid absolute URI."));

        if (accessTokenLifetime <= 0)
        {
            validation = validation.Combine(Result.Failure(
                "client.access_token_lifetime.invalid",
                "Access token lifetime must be greater than zero."));
        }

        if (authorizationCodeLifetime <= 0)
        {
            validation = validation.Combine(Result.Failure(
                "client.authorization_code_lifetime.invalid",
                "Authorization code lifetime must be greater than zero."));
        }

        if (refreshTokenExpiration <= 0)
        {
            validation = validation.Combine(Result.Failure(
                "client.refresh_token_expiration.invalid",
                "Refresh token expiration must be greater than zero."));
        }

        if (!Enum.IsDefined(refreshTokenDeliveryMode))
        {
            validation = validation.Combine(Result.Failure(
                "client.refresh_token_delivery_mode.invalid",
                "Refresh token delivery mode is invalid."));
        }

        return validation;
    }

    private static Result ValidateCibaSettings(
        CibaTokenDeliveryModes backchannelTokenDeliveryMode,
        int cibaDefaultExpirySeconds,
        int cibaMinIntervalSeconds,
        bool allowCibaLoginHint,
        bool allowCibaLoginHintToken,
        bool allowCibaIdTokenHint)
    {
        var validation = Result.Success(0);

        if (backchannelTokenDeliveryMode != CibaTokenDeliveryModes.Poll)
        {
            validation = validation.Combine(Result.Failure(
                "client.ciba.delivery_mode.invalid",
                "Only Poll delivery mode is currently supported."));
        }

        if (cibaDefaultExpirySeconds <= 0)
        {
            validation = validation.Combine(Result.Failure(
                "client.ciba.expiry.invalid",
                "CIBA default expiry must be greater than zero."));
        }

        if (cibaMinIntervalSeconds <= 0)
        {
            validation = validation.Combine(Result.Failure(
                "client.ciba.interval.invalid",
                "CIBA minimum interval must be greater than zero."));
        }

        if (!allowCibaLoginHint && !allowCibaLoginHintToken && !allowCibaIdTokenHint)
        {
            validation = validation.Combine(Result.Failure(
                "client.ciba.hints.invalid",
                "At least one CIBA user hint method must be enabled."));
        }

        return validation;
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }

    private static Result ValidateOptionalAbsoluteUri(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _)
            ? Result.Success(0)
            : Result.Failure(code, message);
    }
}
