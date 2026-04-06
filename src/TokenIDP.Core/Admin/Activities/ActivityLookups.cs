namespace TokenIDP.Core.Admin.Activities;

internal sealed class ActivityLookups
{
    public List<LookupItem> EventTypes { get; init; } = new();
    public List<LookupItem> ActorTypes { get; init; } = new();
}
