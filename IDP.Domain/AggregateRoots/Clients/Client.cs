namespace IDP.Domain.AggregateRoots.Clients;

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
    public bool IsActive { get; private set; }
    public int? ClientSecretExpiry { get; private set; }
    public int AccessTokenLifetime { get; private set; }
    public int AuthorizationCodeLifetime { get; private set; }
    public int RefreshTokenExpiration { get; private set; }
    public int? PermitLimit { get; private set; }
    public TimeSpan? TimeWindow { get; private set; }
    public int? QueueLimit { get; private set; }
    public bool? EnableITracking { get; private set; }

    public virtual Tenant Tenant { get; private set; } = default!;
    public virtual ClientAuthPolicy ClientAuthPolicy { get; private set; } = default!;
    public virtual ICollection<ClientExternalProvider> ClientExternalProviders { get; private set; } = default!;
    public virtual ICollection<ClientScope> ClientScopes { get; private set; } = default!;
    public virtual ICollection<ClientGrantType> ClientGrantTypes { get; private set; } = default!;
    public virtual ICollection<ClientSecret> ClientSecrets { get; private set; } = default!;
    public virtual ICollection<ClientAudience> ClientAudiences { get; private set; } = default!;
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
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking)
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
        ClientSecretExpiry = clientSecretExpiry;
        AccessTokenLifetime = accessTokenLifetime;
        AuthorizationCodeLifetime = authorizationCodeLifetime;
        RefreshTokenExpiration = refreshTokenExpiration;
        PermitLimit = permitLimit;
        TimeWindow = timeWindow;
        QueueLimit = queueLimit;
        EnableITracking = enableITracking;

        ClientScopes = new List<ClientScope>();
        ClientGrantTypes = new List<ClientGrantType>();
        ClientSecrets = new List<ClientSecret>();
        ClientAudiences = new List<ClientAudience>();
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
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking)
    {
        var validation = ValidateInput(
            ClientId,
            clientName,
            redirectUri,
            accessTokenLifetime,
            authorizationCodeLifetime,
            refreshTokenExpiration);
        if (!validation.IsSuccess)
        {
            return validation;
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
        PermitLimit = permitLimit;
        TimeWindow = timeWindow;
        QueueLimit = queueLimit;
        EnableITracking = enableITracking;

        return Result.Success(Id);
    }

    public Result AddSecret(ClientSecret clientSecret)
    {
        if (clientSecret == null)
        {
            return Result.Failure("client.secret.invalid", "Client secret cannot be empty.");
        }

        ClientSecrets.Add(clientSecret);
        return Result.Success(Id);
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

    public Result ReplaceAudiences(IEnumerable<ClientAudience> audiences)
    {
        if (audiences == null)
        {
            return Result.Success(Id);
        }

        ClientAudiences.Clear();
        foreach (var audience in audiences)
        {
            ClientAudiences.Add(audience);
        }

        return Result.Success(Id);
    }

    public Result ConfigureAuthPolicy(
        bool allowLocalLoginOverride,
        bool allowSelfRegistrationOverride,
        bool mfaPolicyOverride,
        bool showExternalProviders,
        bool showStaySignedIn,
        bool showCreateAccountLink)
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
                showCreateAccountLink);
        }
        else
        {
            ClientAuthPolicy.update(
                allowLocalLoginOverride,
                allowSelfRegistrationOverride,
                mfaPolicyOverride,
                showExternalProviders,
                showStaySignedIn,
                showCreateAccountLink);
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
        int? permitLimit,
        TimeSpan? timeWindow,
        int? queueLimit,
        bool? enableITracking,
        out Client? client)
    {
        client = null;

        var validation = ValidateInput(
            clientId,
            clientName,
            redirectUri,
            accessTokenLifetime,
            authorizationCodeLifetime,
            refreshTokenExpiration);
        if (!validation.IsSuccess)
        {
            return validation;
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
            permitLimit,
            timeWindow,
            queueLimit,
            enableITracking);

        return Result.Success(0);
    }

    private static Result ValidateInput(
        string clientId,
        string clientName,
        string redirectUri,
        int accessTokenLifetime,
        int authorizationCodeLifetime,
        int refreshTokenExpiration)
    {
        var validation = ValidateRequired(clientId, "client.id.invalid",
                "Client Id cannot be empty.")
            .Combine(ValidateRequired(clientName, "client.name.invalid",
                "Client name cannot be empty."))
            .Combine(ValidateRequired(redirectUri, "client.redirect.invalid",
                "Redirect URI cannot be empty."));

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

        return validation;
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}
