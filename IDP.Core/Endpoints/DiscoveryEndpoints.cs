using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDP.Core.Endpoints;

internal class DiscoveryEndpoints : IEndpointDefinition
{
    private const string DiscoveryPath = "/.well-known/openid-configuration";
    private const string JwksPath = "/.well-known/jwks.json";

    private readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(DiscoveryPath, async (HttpContext http, IConfiguration configuration) =>
        {
            var metadata = BuildDiscoveryDocument(configuration, http);
            await WriteJsonAsync(http, metadata);
        });

        app.MapGet(JwksPath, async (HttpContext http, IConfiguration configuration, IHostEnvironment env) =>
        {
            var jwks = await BuildJwksAsync(configuration, env);
            await WriteJsonAsync(http, jwks);
        });
    }

    private Dictionary<string, object?> BuildDiscoveryDocument(IConfiguration configuration, HttpContext http)
    {
        var issuer = ResolveIssuer(configuration, http);

        var jwksUri = $"{issuer}{JwksPath}";

        return new Dictionary<string, object?>
        {
            ["issuer"] = issuer,
            ["jwks_uri"] = jwksUri,
            ["authorization_endpoint"] = $"{issuer}/authorize",
            ["token_endpoint"] = $"{issuer}/token",
            ["introspect_endpoint"] = $"{issuer}/introspect",
            ["revoke_token_endpoint"] = $"{issuer}/revoke",
            ["userinfo_endpoint"] = $"{issuer}/userinfo",
            ["response_types_supported"] = new[] { "code" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "client_secret_basic", "client_secret_post" },
            ["grant_types_supported"] = new[] { "authorization_code", "client_credentials", "refresh_token", "device_code", "ciba" },
            ["scopes_supported"] = new[] { "openid", "profile", "email", "phone", "offline_access" }
        };
    }

    private string ResolveIssuer(IConfiguration configuration, HttpContext http)
    {
        var issuer = configuration["TokenOptions:Issuer"];

        if (!string.IsNullOrWhiteSpace(issuer))
        {
            return issuer.TrimEnd('/');
        }

        var request = http.Request;

        var baseUrl = $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase}";

        return baseUrl.TrimEnd('/');
    }

    private async Task<string> BuildJwksAsync(IConfiguration configuration, IHostEnvironment environment)
    {
        // Support certificate based keys or PEM/base64 encoded RSA keys
        var certThumb = configuration["TokenOptions:CertificateThumbprint"];
        var certSubject = configuration["TokenOptions:CertificateSubjectName"];

        if (!string.IsNullOrWhiteSpace(certThumb) || !string.IsNullOrWhiteSpace(certSubject))
        {
            var cert = LoadCertificateFromConfig(configuration);

            var rsa = cert.GetRSAPublicKey();

            if (rsa is null) throw new InvalidOperationException("Certificate does not contain RSA public key.");

            var jwk = CreateJwkFromRsa(rsa);

            jwk["x5c"] = new[] { Convert.ToBase64String(cert.RawData) };

            return SerializeJwks(jwk);
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException("Token signing certificate is required in production." +
                " Provide TokenOptions:CertificateThumbprint or TokenOptions:CertificateSubjectName.");
        }

        var keyMaterial = ResolveKeyMaterialFromConfig(configuration);

        using var rsaKey = RSA.Create();

        if (keyMaterial.Contains("BEGIN", StringComparison.Ordinal))
        {
            rsaKey.ImportFromPem(keyMaterial);
        }
        else
        {
            var keyBytes = Convert.FromBase64String(keyMaterial);

            rsaKey.ImportRSAPrivateKey(keyBytes, out _);
        }

        var jwkRSA = CreateJwkFromRsa(rsaKey);

        return SerializeJwks(jwkRSA);
    }

    private string SerializeJwks(Dictionary<string, object> jwk)
    {
        var jwks = new { keys = new[] { jwk } };
        return JsonSerializer.Serialize(jwks, JsonOptions);
    }

    private Dictionary<string, object> CreateJwkFromRsa(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);

        var n = Base64UrlEncode(parameters.Modulus!);

        var e = Base64UrlEncode(parameters.Exponent!);

        var kid = ComputeKid(n, e);

        return new Dictionary<string, object>
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["alg"] = "RS256",
            ["kid"] = kid,
            ["n"] = n,
            ["e"] = e
        };
    }

    private string ComputeKid(string n, string e)
    {
        // compute a key id as base64url(sha256(n || e))
        var bytes = System.Text.Encoding.UTF8.GetBytes(n + "." + e);

        using var sha = SHA256.Create();

        var hash = sha.ComputeHash(bytes);

        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private async Task WriteJsonAsync(HttpContext http, object payload)
    {
        http.Response.ContentType = "application/json; charset=utf-8";

        await http.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private string ResolveKeyMaterialFromConfig(IConfiguration configuration)
    {
        var keyPath = configuration["TokenOptions:KeyPath"];
        var key = configuration["TokenOptions:Key"];

        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            if (!File.Exists(keyPath))
            {
                throw new FileNotFoundException("Token signing key file was not found.", keyPath);
            }

            return File.ReadAllText(keyPath);
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        throw new InvalidOperationException("Token signing key is missing.");
    }

    private X509Certificate2 LoadCertificateFromConfig(IConfiguration configuration)
    {
        var storeName = Enum.TryParse(configuration["TokenOptions:CertificateStoreName"], true, out StoreName parsedStore)
            ? parsedStore
            : StoreName.My;

        var storeLocation = Enum.TryParse(configuration["TokenOptions:CertificateStoreLocation"], true, out StoreLocation parsedLocation)
            ? parsedLocation
            : StoreLocation.CurrentUser;

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        X509Certificate2Collection matches;
        var thumbprint = configuration["TokenOptions:CertificateThumbprint"]?.Replace(" ", string.Empty);

        if (!string.IsNullOrWhiteSpace(thumbprint))
        {
            matches = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Certificate with thumbprint '{thumbprint}' was not found.");
            }

            return matches[0];
        }

        var subjectName = configuration["TokenOptions:CertificateSubjectName"]?.Trim();
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new InvalidOperationException("Certificate thumbprint or subject name is required.");
        }

        matches = store.Certificates
            .Find(X509FindType.FindBySubjectName, subjectName, validOnly: false);

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"Certificate with subject name '{subjectName}' was not found.");
        }

        var candidate = matches
            .OfType<X509Certificate2>()
            .Where(cert => cert.HasPrivateKey)
            .OrderByDescending(cert => cert.NotAfter)
            .FirstOrDefault();

        if (candidate is null)
        {
            throw new InvalidOperationException($"No certificate with subject name '{subjectName}' has a private key.");
        }

        return candidate;
    }
}
