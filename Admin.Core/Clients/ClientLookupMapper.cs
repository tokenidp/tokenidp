using IDP.Domain.AggregateRoots.Emails;

namespace Admin.Core.Clients;

internal static class ClientLookupMapper
{
    public static List<LookupItem> MapAppTypes()
    {
        return Enum.GetValues<ClientTypes>()
            .Select(value => new LookupItem
            {
                Key = ((int)value).ToString(),
                Value = value.ToString()
            })
            .ToList();
    }

    public static List<LookupItem> MapTokenTypes()
    {
        return Enum.GetValues<TokenTypes>()
            .Select(value => new LookupItem
            {
                Key = ((int)value).ToString(),
                Value = value.ToString()
            })
            .ToList();
    }

    public static List<LookupItem> MapClientScopes()
    {
        return StandardScopes.Supported
            .Select(scope => new LookupItem
            {
                Key = scope,
                Value = scope
            })
            .ToList();
    }

    public static List<LookupItem> MapGrantTypes()
    {
        return Enum.GetValues<GrantTypes>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value switch
                {
                    GrantTypes.authorization_code => "Authorization Code",
                    GrantTypes.client_credentials => "Client Credentials",
                    GrantTypes.refresh_token => "Refresh Token",
                    GrantTypes.device_code => "Device Code",
                    GrantTypes.ciba => "Ciba",
                    _ => value.ToString()
                }
            })
            .ToList();
    }

    public async static Task<List<LookupItem>> MapExternalProviders(int tenantId, IApplicationDbContext db)
    {
        var providers = await db.TenantExternalProviders
            .Where(t => t.TenantId == tenantId && t.Enabled == true)
            .Select(x => new { x.Id, x.ProviderType })
            .ToListAsync();

        if (!providers.Any())
        {
            return new List<LookupItem>();
        }

        return providers
            .Select(value => new LookupItem
            {
                Key = value.Id.ToString(),
                Value = value.ProviderType.ToString()
            })
            .ToList();
    }
}