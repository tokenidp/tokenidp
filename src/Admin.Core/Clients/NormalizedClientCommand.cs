namespace Admin.Core.Clients;

internal sealed class NormalizedClientCommand
{
    private NormalizedClientCommand(
        int tenantId,
        CreateUpdateClient request,
        ClientAuthPolicyDetail authPolicy,
        IReadOnlyList<string> scopes,
        IReadOnlyList<string> apiResources,
        IReadOnlyList<GrantTypes> grantTypes,
        IReadOnlyList<int> selectedProviderIds)
    {
        TenantId = tenantId;
        Request = request;
        AuthPolicy = authPolicy;
        Scopes = scopes;
        ApiResources = apiResources;
        GrantTypes = grantTypes;
        SelectedProviderIds = selectedProviderIds;
    }

    public int TenantId { get; }
    public CreateUpdateClient Request { get; }
    public ClientAuthPolicyDetail AuthPolicy { get; }
    public IReadOnlyList<string> Scopes { get; }
    public IReadOnlyList<string> ApiResources { get; }
    public IReadOnlyList<GrantTypes> GrantTypes { get; }
    public IReadOnlyList<int> SelectedProviderIds { get; }

    public static NormalizedClientCommand Create(CreateUpdateClient request, int tenantId)
    {
        var authPolicy = request.AuthPolicy ?? new ClientAuthPolicyDetail();

        return new NormalizedClientCommand(
            tenantId,
            request,
            authPolicy,
            NormalizeDistinctNames(request.Scopes).ToArray(),
            NormalizeDistinctNames(request.ApiResources).ToArray(),
            (request.GrantTypes ?? new List<GrantTypes>()).Distinct().ToArray(),
            authPolicy.ShowExternalProviders
                ? (request.ExternalProviders ?? new List<int>())
                    .Distinct()
                    .ToArray()
                : Array.Empty<int>());
    }

    private static IEnumerable<string> NormalizeDistinctNames(IEnumerable<string>? values)
    {
        return (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal);
    }
}