using TokenIDP.Core.Foundation.Security;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using TokenOptions = TokenIDP.Core.Foundation.Options.TokenOptions;

namespace TokenIDP.Core.OAuth.Security;

internal sealed class JwtTokenGenerator
{
    private readonly TokenOptions _settings;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SecurityKey _signingKey;
    private readonly SigningCredentials _signingCredentials;
    public JwtTokenGenerator(
        IOptions<TokenOptions> settings,
        ICurrentUserService currentUserService)
    {
        _settings = settings.Value;
        _currentUserService = currentUserService;

        if (TokenSigningMaterialResolver.HasCertificateConfiguration(_settings))
        {
            var certificate = TokenSigningMaterialResolver.LoadCertificate(_settings, requirePrivateKey: true);
            _signingKey = new X509SecurityKey(certificate);
        }
        else
        {
            var keyMaterial = TokenSigningMaterialResolver.ResolveKeyMaterial(_settings);
            _signingKey = CreateSigningKey(keyMaterial);
        }
        _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
    }

    /// <summary>
    /// Create Access Tokens
    /// </summary>
    /// <param name="tokenExpiryInMinutes">Set minutes here for token expiry i.e. 60</param>
    /// <param name="tokenId">Jti claim</param>
    /// <param name="userId">Sub claim</param>
    /// <param name="userName">UniqueName claim</param>
    /// <param name="tenantId">uid claim</param>
    /// <param name="clientId">Audience</param>
    /// <param name="scope">OpenID Connect scopes (profile email openid)</param>
    /// <param name="roles">User roles</param>
    /// <returns>Access Token</returns>
    internal string CreateAccessToken(
        DateTime expireAt,
        string tokenId,
        string clientId,
        int? userId,
        string userName,
        int activeTenantId,
        string activeTenantKey,
        int authTenantId,
        string authTenantKey,
        string[] audiences,
        string[]? scopes,
        IEnumerable<string>? roles)
    {
        var claims = new List<Claim>()
        {
            new(
                JwtRegisteredClaimNames.Sub,
                userId == null
                    ? $"cli:{clientId}"
                    : $"usr:{userId.Value}"),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new("client_id", clientId),
            new("tenant_id", activeTenantId.ToString()),
            new("tenant_key", activeTenantKey),
            new("auth_tenant_id", authTenantId.ToString()),
            new("auth_tenant_key", authTenantKey),
        };

        if (userId != null)
        {
            claims.Add(new Claim("user_id", userId.Value.ToString()));
        }

        if (scopes != null)
        {
            claims.Add(new("scope", string.Join(" ", scopes)));
        }

        AddIfPresent(claims, JwtRegisteredClaimNames.UniqueName, userName);

        // Add roles if present
        if (roles is not null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        return CreateToken(claims, audiences, expireAt);
    }

    /// <summary>
    /// Create ID Tokens
    /// </summary>
    /// <param name="tokenExpiryInMinutes">Set minutes here for token expiry i.e. 60</param>
    /// <param name="tokenId">Jti claim</param>
    /// <param name="userId">Sub claim</param>
    /// <param name="userName">UniqueName claim</param>
    /// <param name="tenantId">uid claim</param>
    /// <param name="clientId">Audience</param>
    /// <param name="scope">OpenID Connect scopes (profile email openid)</param>
    /// <param name="roles">User roles</param>
    /// <returns>ID Token</returns>
    internal string CreateIDToken(
        DateTime expireAt,
        string tokenId,
        string clientId,
        int? userId,
        string userName,
        int activeTenantId,
        string activeTenantKey,
        int authTenantId,
        string authTenantKey,
        string[] audiences)
    {
        return CreateIDToken(
            expireAt,
            tokenId,
            clientId,
            userId,
            activeTenantId,
            activeTenantKey,
            authTenantId,
            authTenantKey,
            audiences,
            name: userName,
            email: null,
            emailVerified: null,
            phoneNumber: null);
    }

    /// <summary>
    /// Create ID Token with OIDC-standard claims.
    /// </summary>
    /// <param name="tokenExpiryInMinutes">Set minutes here for token expiry i.e. 60</param>
    /// <param name="tokenId">Jti claim (optional)</param>
    /// <param name="userId">Sub claim</param>
    /// <param name="clientId">Audience</param>
    /// <param name="name">Name claim</param>
    /// <param name="email">Email claim</param>
    /// <param name="emailVerified">Email verified claim</param>
    /// <param name="phoneNumber">Phone number claim</param>
    /// <param name="picture">Picture URL claim</param>
    /// <returns>ID Token</returns>
    internal string CreateIDToken(
        DateTime expireAt,
        string? tokenId,
        string clientId,
        int? userId,
        int activeTenantId,
        string activeTenantKey,
        int authTenantId,
        string authTenantKey,
        string[] audiences,
        string? name,
        string? email,
        bool? emailVerified,
        string? phoneNumber)
    {
        var claims = new List<Claim>()
        {
            new(
                JwtRegisteredClaimNames.Sub,
                userId == null
                    ? $"cli:{clientId}"
                    : $"usr:{userId.Value}"),
            new("client_id", clientId),
            new("tenant_id", activeTenantId.ToString()),
            new("tenant_key", activeTenantKey),
            new("auth_tenant_id", authTenantId.ToString()),
            new("auth_tenant_key", authTenantKey)
        };

        if (!string.IsNullOrWhiteSpace(tokenId))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenId));
        }

        if (userId != null)
        {
            claims.Add(new Claim("user_id", userId.Value.ToString()));
        }

        AddIfPresent(claims, JwtRegisteredClaimNames.Name, name);
        AddIfPresent(claims, JwtRegisteredClaimNames.Email, email);

        if (emailVerified is not null)
        {
            claims.Add(new Claim("email_verified", emailVerified.Value ? "true" : "false"));
        }

        AddIfPresent(claims, JwtRegisteredClaimNames.PhoneNumber, phoneNumber);

        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ResolveIssuer(),
            Audience = audiences[0],
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expireAt,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(descriptor);

        return _tokenHandler.WriteToken(token);
    }

    private string CreateToken(IEnumerable<Claim> claims, string[] audiences, DateTime expireAt)
    {
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ResolveIssuer(),
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expireAt,
            SigningCredentials = _signingCredentials
        };

        if (audiences.Length == 1)
        {
            descriptor.Audience = audiences[0];
        }
        else if (audiences.Length > 1)
        {
            foreach (var aud in audiences)
            {
                descriptor.Audiences.Add(aud);
            }
        }

        var token = _tokenHandler.CreateToken(descriptor);

        return _tokenHandler.WriteToken(token);
    }

    private void AddIfPresent(ICollection<Claim> claims, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }

    private string ResolveIssuer()
    {
        return !string.IsNullOrWhiteSpace(_currentUserService.BaseUrl)
            ? _currentUserService.BaseUrl.TrimEnd('/')
            : TokenOptionsResolver.ResolveIssuer(_settings);
    }

    private SecurityKey CreateSigningKey(string keyMaterial)
    {
        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            throw new InvalidOperationException("Token signing key is missing.");
        }

        var rsa = RSA.Create();

        if (keyMaterial.Contains("BEGIN", StringComparison.Ordinal))
        {
            rsa.ImportFromPem(keyMaterial);
            return new RsaSecurityKey(rsa);
        }

        try
        {
            var keyBytes = Convert.FromBase64String(keyMaterial);
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            return new RsaSecurityKey(rsa);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Token signing key must be PEM or base64-encoded RSA private key.", ex);
        }
    }
}
