using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using TokenIDP.Core.Foundation.Security;
using TokenOptions = TokenIDP.Core.Foundation.Options.TokenOptions;

namespace TokenIDP.Core.OAuth.Endpoints;

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
        app.MapGet(DiscoveryPath, async (HttpContext http, IOptions<TokenOptions> tokenOptions) =>
        {
            var issuer = ResolveIssuer(http, tokenOptions.Value);
            var metadata = BuildDiscoveryDocument(issuer);
            await WriteJsonAsync(http, metadata);
        });

        app.MapGet(JwksPath, async (HttpContext http, IOptions<TokenOptions> tokenOptions, IHostEnvironment env) =>
        {
            var jwks = BuildJwksAsync(tokenOptions.Value, env);
            await WriteJsonAsync(http, jwks);
        });
    }

    private Dictionary<string, object?> BuildDiscoveryDocument(string issuer)
    {
        var jwksUri = $"{issuer}{JwksPath}";

        return new Dictionary<string, object?>
        {
            ["issuer"] = issuer,
            ["jwks_uri"] = jwksUri,
            ["authorization_endpoint"] = $"{issuer}/authorize",
            ["token_endpoint"] = $"{issuer}/token",
            ["device_authorization_endpoint"] = $"{issuer}/device_authorization",
            ["backchannel_authentication_endpoint"] = $"{issuer}/backchannel-authentication",
            ["introspect_endpoint"] = $"{issuer}/introspect",
            ["revoke_token_endpoint"] = $"{issuer}/revoke",
            ["userinfo_endpoint"] = $"{issuer}/userinfo",
            ["backchannel_token_delivery_modes_supported"] = new[] { "poll" },
            ["backchannel_user_code_parameter_supported"] = true,
            ["response_types_supported"] = new[] { "code" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "client_secret_basic", "client_secret_post", "none" },
            ["grant_types_supported"] = SupportedTokenGrantTypes.Names,
            ["scopes_supported"] = new[] { "openid", "profile", "email", "phone", "offline_access" }
        };
    }

    private string BuildJwksAsync(TokenOptions settings, IHostEnvironment environment)
    {
        if (TokenSigningMaterialResolver.HasCertificateConfiguration(settings))
        {
            var cert = TokenSigningMaterialResolver.LoadCertificate(settings);

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

        var keyMaterial = TokenSigningMaterialResolver.ResolveKeyMaterial(settings);

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

    private static string ResolveIssuer(HttpContext httpContext, TokenOptions tokenOptions)
    {
        return httpContext.Request.Host.HasValue
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}".TrimEnd('/')
            : TokenOptionsResolver.ResolveIssuer(tokenOptions);
    }

}

