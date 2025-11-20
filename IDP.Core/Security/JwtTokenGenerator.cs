using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IDP.Core.Security;

public class JwtTokenGenerator
{
    private readonly TokenSetting _jwtSettings;

    public JwtTokenGenerator(IOptions<TokenSetting> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GetAccessToken(string jti,
        string sub,
        string name,
        string tenantId,
        string audience,
        string scope,
        string[] roles)
    {
        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, sub),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.UniqueName, name),
            new Claim("uid", tenantId),
            new Claim("scope", scope)
        };

        if (roles?.Any() == true)
        {
            var roleClaims = new List<Claim>();

            for (int i = 0; i < roles.Length; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }

            claims.AddRange(roleClaims);
        }

        var symmetricSecurityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));

        var signingCredentials = new SigningCredentials(
            symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: audience,
            claims: claims.ToArray(),
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        return token;
    }

    public static string GetRefreshToken()
    {
        string token = string.Empty;
        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            var randomBytes = new byte[64];
            randomNumberGenerator.GetBytes(randomBytes);
            token = Convert.ToBase64String(randomBytes);
        }

        return token;
    }
}
