using IDP.Domain.AggregateRoots.Permissions;

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
            }).ToList();
    }
}