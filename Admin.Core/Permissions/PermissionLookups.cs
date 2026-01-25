namespace Admin.Core.Permissions;

internal sealed class PermissionLookups
{
    public List<LookupItem> ParentMenus { get; init; } = new();
    public List<LookupItem> ControlTypes { get; init; } = new();
}


