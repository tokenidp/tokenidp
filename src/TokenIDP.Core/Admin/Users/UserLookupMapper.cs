namespace TokenIDP.Core.Admin.Permissions;

public static class UserLookupMapper
{
    public static List<LookupItem> MapUserStatuses()
    {
        return Enum.GetValues<UserStatus>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            })
            .ToList();
    }

    public static List<LookupItem> MapAddressTypes()
    {
        return Enum.GetValues<AddressTypes>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            })
            .ToList();
    }
}
