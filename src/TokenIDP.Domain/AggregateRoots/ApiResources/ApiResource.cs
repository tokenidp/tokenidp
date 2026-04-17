namespace TokenIDP.Domain.AggregateRoots.ApiResources;

public sealed class ApiResource : AggregateRoot<Guid>, ITenant
{
    private readonly List<ApiScope> _scopes = new();

    public int TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Enabled { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<ApiScope> Scopes => _scopes.AsReadOnly();

    private ApiResource() { }

    private ApiResource(int tenantId, string name, string displayName, string? description, bool enabled)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name;
        DisplayName = displayName;
        Description = description;
        Enabled = enabled;
        IsDeleted = false;
    }

    public static Result Create(
        int tenantId,
        string name,
        string displayName,
        string? description,
        bool enabled,
        out ApiResource? apiResource)
    {
        apiResource = null;

        var validation = ValidateRequired(name, "api_resource.name.invalid", "ApiResource name cannot be empty.")
            .Combine(ValidateRequired(displayName, "api_resource.display_name.invalid", "ApiResource display name cannot be empty."));

        if (!validation.IsSuccess)
        {
            return validation;
        }

        apiResource = new ApiResource(
            tenantId,
            name.Trim(),
            displayName.Trim(),
            description?.Trim(),
            enabled);

        return Result.Success(0);
    }

    public Result Update(string name, string displayName, string? description, bool enabled)
    {
        if (IsDeleted)
        {
            return Result.Failure(
                "api_resource.deleted",
                "Deleted ApiResource cannot be modified.");
        }

        var validation = ValidateRequired(name, "api_resource.name.invalid", "ApiResource name cannot be empty.")
            .Combine(ValidateRequired(displayName, "api_resource.display_name.invalid", "ApiResource display name cannot be empty."));

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

    public Result ReplaceScopes(IEnumerable<ApiScope> scopes)
    {
        if (IsDeleted)
        {
            return Result.Failure(
                "api_resource.deleted",
                "Deleted ApiResource cannot be modified.");
        }

        _scopes.Clear();

        if (scopes == null)
        {
            return Result.Success(0);
        }

        foreach (var scope in scopes)
        {
            _scopes.Add(scope);
        }

        return Result.Success(0);
    }

    public Result SoftDelete()
    {
        if (IsDeleted)
        {
            return Result.Failure(
                "api_resource.deleted",
                "ApiResource is already deleted.");
        }

        IsDeleted = true;
        Enabled = false;

        return Result.Success(0);
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}

