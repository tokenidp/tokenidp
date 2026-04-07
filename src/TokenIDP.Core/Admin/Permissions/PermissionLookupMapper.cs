using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Core.Admin.Permissions;

public static class PermissionLookupMapper
{
    public static List<LookupItem> MapControlTypes()
    {
        return Enum.GetValues<ControlTypes>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            }).ToList();
    }
}
