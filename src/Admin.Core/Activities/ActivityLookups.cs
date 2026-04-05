namespace Admin.Core.Activities;

internal sealed class ActivityLookups
{
    public List<LookupItem> EventTypes { get; init; } = new();
    public List<LookupItem> ActorTypes { get; init; } = new();
}