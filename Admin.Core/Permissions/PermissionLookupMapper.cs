namespace Admin.Core.Permissions;

internal static class PermissionLookupMapper
{
    public static List<LookupItem> MapControlTypes()
    {
        return Enum.GetValues<ControlTypes>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            }).Where(s => s.Key != "ApiResource" && s.Key != "ApiScope")
            .ToList();
    }
}