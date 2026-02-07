namespace Admin.Core.Clients;

internal static class ClientLookupMapper
{
    public static List<LookupItem> MapAppTypes()
    {
        return Enum.GetValues<AppTypes>()
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
}