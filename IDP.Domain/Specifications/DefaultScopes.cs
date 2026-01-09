namespace IDP.Domain.Specifications;

public static class StandardScopes
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>
        {
            OpenId,
            Profile,
            Email,
            OfflineAccess
        };
}
