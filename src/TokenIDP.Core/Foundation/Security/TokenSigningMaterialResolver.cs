using TokenIDP.Core.Foundation.Options;
using System.Security.Cryptography.X509Certificates;

namespace TokenIDP.Core.Foundation.Security;

public static class TokenSigningMaterialResolver
{
    public static bool HasCertificateConfiguration(TokenOption settings)
    {
        return !string.IsNullOrWhiteSpace(settings.CertificateThumbprint) ||
               !string.IsNullOrWhiteSpace(settings.CertificateSubjectName);
    }

    public static string ResolveKeyMaterial(TokenOption settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.KeyPath))
        {
            if (!File.Exists(settings.KeyPath))
            {
                throw new FileNotFoundException("Token signing key file was not found.", settings.KeyPath);
            }

            return File.ReadAllText(settings.KeyPath);
        }

        if (!string.IsNullOrWhiteSpace(settings.Key))
        {
            return settings.Key;
        }

        throw new InvalidOperationException("Token signing key is missing.");
    }

    public static X509Certificate2 LoadCertificate(TokenOption settings, bool requirePrivateKey = false)
    {
        var storeName = Enum.TryParse(settings.CertificateStoreName, true, out StoreName parsedStore)
            ? parsedStore
            : StoreName.My;

        var storeLocation = Enum.TryParse(settings.CertificateStoreLocation, true, out StoreLocation parsedLocation)
            ? parsedLocation
            : StoreLocation.CurrentUser;

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var thumbprint = settings.CertificateThumbprint?.Replace(" ", string.Empty);

        if (!string.IsNullOrWhiteSpace(thumbprint))
        {
            var matches = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Certificate with thumbprint '{thumbprint}' was not found.");
            }

            var certificate = matches[0];

            if (requirePrivateKey && !certificate.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    $"Certificate with thumbprint '{thumbprint}' does not have an accessible private key.");
            }

            return certificate;
        }

        var subjectName = settings.CertificateSubjectName?.Trim();

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new InvalidOperationException("Certificate thumbprint or subject name is required.");
        }

        var subjectMatches = store.Certificates
            .Find(X509FindType.FindBySubjectName, subjectName, validOnly: false)
            .OfType<X509Certificate2>();

        var candidate = subjectMatches
            .Where(cert => cert.HasPrivateKey)
            .OrderByDescending(cert => cert.NotAfter)
            .FirstOrDefault();

        if (candidate is not null)
        {
            return candidate;
        }

        if (requirePrivateKey)
        {
            throw new InvalidOperationException($"No certificate with subject name '{subjectName}' has a private key.");
        }

        candidate = subjectMatches
            .OrderByDescending(cert => cert.NotAfter)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new InvalidOperationException($"Certificate with subject name '{subjectName}' was not found.");
        }

        return candidate;
    }
}
