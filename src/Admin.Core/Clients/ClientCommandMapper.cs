using IDP.Foundation.Security;

namespace Admin.Core.Clients;

internal static class ClientCommandMapper
{
    public static Result BuildChanges(
        NormalizedClientCommand command,
        out ClientCommandChanges? changes)
    {
        changes = null;

        var scopeResult = BuildScopes(command.Scopes, out var scopes);
        if (!scopeResult.IsSuccess)
        {
            return scopeResult;
        }

        var grantResult = BuildGrantTypes(command.GrantTypes, out var grantTypes);
        if (!grantResult.IsSuccess)
        {
            return grantResult;
        }

        var apiResourceResult = BuildApiResources(command.ApiResources, out var apiResources);
        if (!apiResourceResult.IsSuccess)
        {
            return apiResourceResult;
        }

        var secretResult = BuildSecret(
            command.Request.ClientSecret,
            command.Request.ClientSecretDescription,
            command.Request.ClientSecretExpiry,
            out var clientSecret);
        if (!secretResult.IsSuccess)
        {
            return secretResult;
        }

        changes = new ClientCommandChanges
        {
            Scopes = scopes,
            GrantTypes = grantTypes,
            ApiResources = apiResources,
            ClientSecret = clientSecret
        };

        return Result.Success(0);
    }

    public static Result ApplyToClient(
        Client client,
        NormalizedClientCommand command,
        ClientCommandChanges changes)
    {
        client.ReplaceScopes(changes.Scopes);
        client.ReplaceGrantTypes(changes.GrantTypes);
        client.ReplaceApiResources(changes.ApiResources);

        if (changes.ClientSecret != null)
        {
            client.AddSecret(changes.ClientSecret);
        }

        var authPolicy = command.AuthPolicy;
        var authPolicyResult = client.ConfigureAuthPolicy(
            authPolicy.AllowLocalLoginOverride,
            authPolicy.AllowSelfRegistrationOverride,
            authPolicy.MfaPolicyOverride,
            authPolicy.ShowExternalProviders,
            authPolicy.ShowStaySignedIn,
            authPolicy.ShowCreateAccountLink,
            authPolicy.AutoCreateUsers,
            authPolicy.DefaultRoleId);
        if (!authPolicyResult.IsSuccess)
        {
            return authPolicyResult;
        }

        return client.ReplaceExternalProviders(command.SelectedProviderIds);
    }

    private static Result BuildSecret(
        string? secret,
        string? description,
        int? clientSecretExpiry,
        out ClientSecret? mapped)
    {
        mapped = null;

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result.Success(0);
        }

        var hash = SecretHasher.HashSecret(secret);
        var expiresAt = clientSecretExpiry.HasValue
            ? DateTime.UtcNow.AddDays(clientSecretExpiry.Value)
            : (DateTime?)null;

        return ClientSecret.Create(hash, description, expiresAt, out mapped);
    }

    private static Result BuildScopes(
        IEnumerable<string> scopes,
        out List<ClientScope> mapped)
    {
        mapped = new List<ClientScope>();

        var combined = Result.Success(0);
        foreach (var scope in scopes)
        {
            var result = ClientScope.Create(scope, out var created);
            if (!result.IsSuccess)
            {
                combined = combined.Combine(result);
                continue;
            }

            if (created != null)
            {
                mapped.Add(created);
            }
        }

        return combined;
    }

    private static Result BuildGrantTypes(
        IEnumerable<GrantTypes> grantTypes,
        out List<ClientGrantType> mapped)
    {
        mapped = new List<ClientGrantType>();

        var combined = Result.Success(0);
        foreach (var grantType in grantTypes)
        {
            var result = ClientGrantType.Create(grantType, out var created);
            if (!result.IsSuccess)
            {
                combined = combined.Combine(result);
                continue;
            }

            if (created != null)
            {
                mapped.Add(created);
            }
        }

        return combined;
    }

    private static Result BuildApiResources(
        IEnumerable<string> apiResources,
        out List<ClientApiResource> mapped)
    {
        mapped = new List<ClientApiResource>();

        var combined = Result.Success(0);
        foreach (var apiResource in apiResources)
        {
            var result = ClientApiResource.Create(apiResource, true, out var created);
            if (!result.IsSuccess)
            {
                combined = combined.Combine(result);
                continue;
            }

            if (created != null)
            {
                mapped.Add(created);
            }
        }

        return combined;
    }
}