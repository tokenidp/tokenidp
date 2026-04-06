namespace TokenIDP.Core.Admin.ApiResources;

public sealed class CreateUpdateApiResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
    public List<CreateUpdateApiScope> Scopes { get; set; } = new();
}

public sealed class CreateUpdateApiScope
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
}

