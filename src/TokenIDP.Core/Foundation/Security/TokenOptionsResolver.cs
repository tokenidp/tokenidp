using TokenOptions = TokenIDP.Core.Foundation.Options.TokenOptions;

namespace TokenIDP.Core.Foundation.Security;

public static class TokenOptionsResolver
{
    public static string ResolveIssuer(TokenOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.Issuer))
        {
            throw new InvalidOperationException("Token issuer is required. Configure TokenOptions:Issuer.");
        }

        return settings.Issuer.TrimEnd('/');
    }

    public static string ResolveAudience(TokenOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new InvalidOperationException("Token audience is required. Configure TokenOptions:Audience.");
        }

        return settings.Audience.Trim();
    }
}
