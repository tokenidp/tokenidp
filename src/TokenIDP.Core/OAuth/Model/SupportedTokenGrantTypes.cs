namespace TokenIDP.Core.OAuth.Model;

internal static class SupportedTokenGrantTypes
{
    private static readonly GrantTypes[] SupportedGrantTypes =
    [
        GrantTypes.authorization_code,
        GrantTypes.refresh_token,
        GrantTypes.client_credentials,
        GrantTypes.device_code,
        GrantTypes.ciba,
        GrantTypes.password
    ];

    public static IReadOnlyList<string> Names { get; } = SupportedGrantTypes
        .Select(TokenGrantTypeNames.GetName)
        .ToArray();

    public static bool IsSupported(GrantTypes grantType)
    {
        return SupportedGrantTypes.Contains(grantType);
    }
}

