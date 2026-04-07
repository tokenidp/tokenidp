namespace TokenIDP.Core.Admin;

public sealed class ApiResourceValidationItem
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> ScopeNames { get; init; } = Array.Empty<string>();
}

public sealed class RoleAssignmentValidation
{
    public bool Exists { get; init; }
    public bool IsActive { get; init; }
    public bool IsAssignableToNewUsers { get; init; }
}
