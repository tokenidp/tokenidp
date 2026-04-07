namespace TokenIDP.Core.Admin.ApiResources;

public sealed class ApiResourceDetail
{
    public static Expression<Func<ApiResource, ApiResourceDetail>> Projection =>
        apiResource => new ApiResourceDetail
        {
            Id = apiResource.Id,
            Name = apiResource.Name,
            DisplayName = apiResource.DisplayName,
            Description = apiResource.Description,
            Enabled = apiResource.Enabled,
            Scopes = apiResource.Scopes
                .OrderBy(scope => scope.DisplayName)
                .Select(scope => new ApiScopeDetail
                {
                    Id = scope.Id,
                    Name = scope.Name,
                    DisplayName = scope.DisplayName,
                    Description = scope.Description,
                    Enabled = scope.Enabled
                })
                .ToList()
        };

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Enabled { get; private set; }
    public List<ApiScopeDetail> Scopes { get; private set; } = new();
}

public sealed class ApiScopeDetail
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Enabled { get; init; }
}

