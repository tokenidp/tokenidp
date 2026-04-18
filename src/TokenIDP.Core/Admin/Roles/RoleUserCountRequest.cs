namespace TokenIDP.Core.Admin.Roles;

public sealed class RoleUserCountRequest
{
    public IReadOnlyList<int> RoleIds { get; init; } = Array.Empty<int>();
}
