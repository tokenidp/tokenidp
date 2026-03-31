namespace IDP.Domain.AggregateRoots.ApiResources;

public sealed class ApiScope : AggregateRoot<Guid>
{
    public Guid ApiResourceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Enabled { get; private set; }

    public ApiResource ApiResource { get; private set; } = default!;

    private ApiScope() { }

    private ApiScope(string name, string displayName, string? description, bool enabled)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayName = displayName;
        Description = description;
        Enabled = enabled;
    }

    public static Result Create(
        string name,
        string displayName,
        string? description,
        bool enabled,
        out ApiScope? apiScope)
    {
        apiScope = null;

        var validation = ValidateRequired(name, "api_scope.name.invalid", "ApiScope name cannot be empty.")
            .Combine(ValidateRequired(displayName, "api_scope.display_name.invalid", "ApiScope display name cannot be empty."));

        if (!validation.IsSuccess)
        {
            return validation;
        }

        apiScope = new ApiScope(
            name.Trim(),
            displayName.Trim(),
            description?.Trim(),
            enabled);

        return Result.Success(0);
    }

    public Result Update(string name, string displayName, string? description, bool enabled)
    {
        var validation = ValidateRequired(name, "api_scope.name.invalid", "ApiScope name cannot be empty.")
            .Combine(ValidateRequired(displayName, "api_scope.display_name.invalid", "ApiScope display name cannot be empty."));

        if (!validation.IsSuccess)
        {
            return validation;
        }

        Name = name.Trim();
        DisplayName = displayName.Trim();
        Description = description?.Trim();
        Enabled = enabled;

        return Result.Success(0);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}
