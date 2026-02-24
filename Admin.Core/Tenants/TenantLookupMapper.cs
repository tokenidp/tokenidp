namespace Admin.Core.Tenants;

internal static class TenantLookupMapper
{
    public static List<LookupItem> MapTenantStatuses()
    {
        return new List<LookupItem>
        {
            new()
            {
                Key = bool.TrueString.ToLowerInvariant(),
                Value = "Active"
            },
            new()
            {
                Key = bool.FalseString.ToLowerInvariant(),
                Value = "Inactive"
            }
        };
    }

    public static List<LookupItem> MapExternalProviders()
    {
        return Enum.GetValues<ExternalProviderTypes>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            })
            .ToList();
    }

    public static List<LookupItem> MapAuthenticationModes()
    {
        return Enum.GetValues<AuthenticationModes>()
            .Select(value => new LookupItem
            {
                Key = ((int)value).ToString(),
                Value = value.ToString()
            })
            .ToList();
    }
}
