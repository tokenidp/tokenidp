namespace TokenIDP.Core.OAuth.Model;

internal static class TokenGrantTypeNames
{
    public const string Ciba = "urn:openid:params:grant-type:ciba";

    public static string GetName(GrantTypes grantType)
    {
        return grantType switch
        {
            GrantTypes.ciba => Ciba,
            _ => grantType.ToString()
        };
    }

    public static bool TryParse(string? value, out GrantTypes grantType)
    {
        grantType = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (string.Equals(value, Ciba, StringComparison.OrdinalIgnoreCase))
        {
            grantType = GrantTypes.ciba;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out grantType) &&
               Enum.IsDefined(typeof(GrantTypes), grantType);
    }
}
