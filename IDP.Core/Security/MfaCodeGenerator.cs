using System.Security.Cryptography;

namespace IDP.Core.Security;

internal static class MfaCodeGenerator
{
    internal static string GenerateMfaCode()
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }
        while (IsWeakCode(code));

        return code;
    }

    private static bool IsWeakCode(string code)
    {
        if (code is "000000" or "123456" or "654321" or "111111" or "222222" or "333333" or "444444" or "555555" or "666666" or "777777" or "888888" or "999999")
        {
            return true;
        }

        var ascending = true;
        var descending = true;
        for (var i = 1; i < code.Length; i++)
        {
            ascending &= code[i] == code[i - 1] + 1;
            descending &= code[i] == code[i - 1] - 1;
        }

        return ascending || descending;
    }
}