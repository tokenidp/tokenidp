using IDP.Core.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IDP.Core.Security;

public sealed class JwtTokenGenerator
{
    private readonly TokenOption _settings;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenGenerator(IOptions<TokenOption> settings)
    {
        _settings = settings.Value;
    }

    public string CreateAccessToken(
        string jti,
        string subject,
        string displayName,
        string tenantId,
        string audience,
        string scope,
        IEnumerable<string>? roles)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>(capacity: 8)
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.UniqueName, displayName),
            new("uid", tenantId),
            new("scope", scope)
        };

        // Add roles if present
        if (roles is not null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = now.AddMinutes(_settings.DurationInMinutes),
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(descriptor);

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a secure 512-bit refresh token using non-allocating RNG APIs.
    /// </summary>
    public static string CreateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64]; // 512-bit
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
