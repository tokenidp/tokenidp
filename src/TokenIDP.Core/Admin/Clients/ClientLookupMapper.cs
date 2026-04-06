namespace TokenIDP.Core.Admin.Clients;

internal static class ClientLookupMapper
{
    public static Task<List<ApiResourceLookup>> MapApiResources(
        int tenantId,
        IApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        return db.ApiResources
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Enabled)
            .OrderBy(x => x.DisplayName)
            .Select(x => new ApiResourceLookup
            {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Scopes = x.Scopes
                    .Where(scope => scope.Enabled)
                    .OrderBy(scope => scope.DisplayName)
                    .Select(scope => new ApiScopeLookup
                    {
                        Id = scope.Id,
                        Name = scope.Name,
                        DisplayName = scope.DisplayName
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

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
                    GrantTypes.password => "Resource Owner Password Credentials",
                    _ => value.ToString()
                }
            })
            .ToList();
    }

    public static async Task<List<LookupItem>> MapExternalProviders(int tenantId, IApplicationDbContext db)
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

    public static async Task<List<LookupItem>> MapRoles(int tenantId, IApplicationDbContext db)
    {
        var roles = await db.Roles
            .Where(t =>
                t.TenantId == tenantId &&
                t.IsDeleted != true &&
                t.IsActive &&
                t.IsAssignableToNewUsers)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        if (!roles.Any())
        {
            return new List<LookupItem>();
        }

        return roles
            .Select(value => new LookupItem
            {
                Key = value.Id.ToString(),
                Value = value.Name
            })
            .ToList();
    }
}
