using Admin.Core.Bootstrap;
using Admin.Core.Clients;

namespace IDP.Infrastructure.Bootstrap;

internal class ClientProvisioningService : IClientProvisioningService
{
    public async Task CreateAsync(IApplicationDbContext db,
        int tenantId,
        string clientId,
        CreateUpdateClient command,
        CancellationToken ct)
    {
        var createResult = Client.Create(
            tenantId,
            clientId,
            command.ClientName,
            command.Description,
            command.AppType,
            command.AccessTokenType,
            command.RedirectUri,
            command.LogoutRedirectUri,
            command.IsActive,
            command.ClientSecretExpiry,
            command.AccessTokenLifetime,
            command.AuthorizationCodeLifetime,
            command.RefreshTokenExpiration,
            command.PermitLimit,
            command.TimeWindow,
            command.QueueLimit,
            command.EnableITracking,
            out var client);

        BuildScopes(command.Scopes, out var scopes);

        BuildGrantTypes(command.GrantTypes, out var grants);

        BuildAudiences(command.Audiences, out var audiences);

        client!.ReplaceScopes(scopes);
        client!.ReplaceGrantTypes(grants);
        client!.ReplaceAudiences(audiences);

        var authPolicy = command.AuthPolicy ?? new ClientAuthPolicyDetail();
        client.ConfigureAuthPolicy(
            authPolicy.AllowLocalLoginOverride,
            authPolicy.AllowSelfRegistrationOverride,
            authPolicy.MfaPolicyOverride,
            authPolicy.ShowExternalProviders,
            authPolicy.ShowStaySignedIn,
            authPolicy.ShowCreateAccountLink);
        client.ReplaceExternalProviders(authPolicy.ShowExternalProviders
            ? (command.ExternalProviders ?? new List<int>())
            : new List<int>());

        db.Clients.Add(client!);

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(IApplicationDbContext db,
        int tenantId,
        string clientId,
        CancellationToken ct)
    {
        var isExist = await db.Clients
            .AsNoTracking()
            .AnyAsync(t => t.TenantId == tenantId && t.ClientId == clientId, ct);

        return isExist;
    }

    private static void BuildScopes(IEnumerable<string> scopes,
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
    }

    private static void BuildGrantTypes(IEnumerable<GrantTypes> grantTypes,
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
    }

    private static void BuildAudiences(IEnumerable<string> audiences,
        out List<ClientAudience> mapped)
    {
        mapped = new List<ClientAudience>();

        var combined = Result.Success(0);
        foreach (var audience in audiences)
        {
            var result = ClientAudience.Create(audience, true, out var created);
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
    }
}