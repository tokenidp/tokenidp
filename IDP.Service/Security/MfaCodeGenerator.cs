using System.Security.Cryptography;

namespace IDP.Service.Security;

public static class MfaCodeGenerator
{
    public static string GenerateMfaCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4]; // 4 bytes = 32-bit integer
        rng.GetBytes(bytes);

        int value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Make it non-negative
        int code = value % 1000000; // Limit to 6 digits

        return code.ToString("D6"); // Pad with leading zeros if needed
    }
}