using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TokenIDP.Core.Foundation;

public static class TenantKeyGenerator
{
    private static readonly Regex NonAlphaNumeric = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static string Generate(string tenantName)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(tenantName));

        // Normalize unicode (ä ? a, ü ? u, etc.)
        var normalized = tenantName
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = Char.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var clean = sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        clean = NonAlphaNumeric.Replace(clean, "-");
        clean = clean.Trim('-');
        clean = Regex.Replace(clean, "-{2,}", "-");

        return clean;
    }
}

