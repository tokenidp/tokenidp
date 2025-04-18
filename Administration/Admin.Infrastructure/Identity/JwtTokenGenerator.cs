using Identity.Application.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Identity;

public class JwtTokenGenerator
{
    private readonly JwtSetting _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSetting> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GetAccessToken(string sub, string name, string uId, string[] roles)
    {
        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, sub),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, name),
            new Claim("uid", uId)
        };

        if (roles.IsSafe())
        {
            var roleClaims = new List<Claim>();

            for (int i = 0; i < roles.Length; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }

            claims.AddRange(roleClaims);
        }

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims.ToArray(),
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        return token;
    }

    public static string GetRefreshToken(string ipAddress)
    {
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        randomNumberGenerator.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
