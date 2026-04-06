namespace TokenIDP.Infrastructure.Bootstrap.SeedData;

internal static class DefaultApiResources
{
    public const string AdminApiResourceName = "tokenidp.admin.api";
    public const string AdminReadScopeName = "tokenidp.admin.read";
    public const string AdminWriteScopeName = "tokenidp.admin.write";

    public static DefaultApiResourceDefinition AdminApi { get; } = new(
        AdminApiResourceName,
        "TokenIdP Admin API",
        "Administrative API for the TokenIdP platform.",
        true,
        new[]
        {
            new DefaultApiScopeDefinition(
                AdminReadScopeName,
                "TokenIdP Admin Read",
                "Read access to TokenIdP administrative APIs.",
                true),
            new DefaultApiScopeDefinition(
                AdminWriteScopeName,
                "TokenIdP Admin Write",
                "Write access to TokenIdP administrative APIs.",
                true)
        });
}

internal sealed record DefaultApiResourceDefinition(
    string Name,
    string DisplayName,
    string? Description,
    bool Enabled,
    IReadOnlyCollection<DefaultApiScopeDefinition> Scopes);

internal sealed record DefaultApiScopeDefinition(
    string Name,
    string DisplayName,
    string? Description,
    bool Enabled);
