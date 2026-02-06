using IDP.Foundation.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace IDP.Core.Security;

internal sealed class JwtTokenGenerator
{
    private readonly TokenOption _settings;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SecurityKey _signingKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly ICurrentUserService _currentUserService;

    public JwtTokenGenerator(IOptions<TokenOption> settings,
        ICurrentUserService currentUserService)
    {
        _settings = settings.Value;
        var keyMaterial = GetKeyMaterial(_settings);
        _signingKey = CreateSigningKey(keyMaterial);
        _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
        _currentUserService = currentUserService;
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
        string tenantId,
        string[] audiences,
        string[]? scopes,
        IEnumerable<string>? roles)
    {
        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub, userId == null ? clientId : userId.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new("uid", tenantId),
        };

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
        string[] audiences)
    {
        return CreateIDToken(
            expireAt,
            tokenId,
            clientId,
            userId,
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
        string[] audiences,
        string? name,
        string? email,
        bool? emailVerified,
        string? phoneNumber)
    {
        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub, userId == null ? clientId : userId.Value.ToString())
        };

        if (!string.IsNullOrWhiteSpace(tokenId))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenId));
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
            Issuer = _currentUserService.BaseUrl,
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
            Issuer = _currentUserService.BaseUrl,
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

    private string GetKeyMaterial(TokenOption settings)
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
}