using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace TokenIDP.Core.OAuth.Security;

internal class DeviceCodeGenerator
{
    private static readonly char[] UserCodeCharset =
        "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    // Removed ambiguous chars: I, O, 0, 1

    public static string GenerateDeviceCode()
    {
        // 256-bit entropy
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Base64UrlEncoder.Encode(bytes);
    }

    public static string GenerateUserCode(int length = 8)
    {
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        var sb = new StringBuilder(length + 1);

        for (int i = 0; i < length; i++)
        {
            var index = randomBytes[i] % UserCodeCharset.Length;
            sb.Append(UserCodeCharset[index]);

            // Optional dash formatting for readability (e.g. XXXX-XXXX)
            if (i == 3 && length >= 8)
                sb.Append('-');
        }

        return sb.ToString();
    }
}

