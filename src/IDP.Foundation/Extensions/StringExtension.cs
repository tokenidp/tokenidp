using System.Text.RegularExpressions;

namespace IDP.Foundation.Extensions;

public static class StringExtension
{
    /// <summary>
    /// this will replace the formatted value with argument repectively like 
    /// api/example/{companyId} is input value and output value will be api/example/1
    /// </summary>
    /// <param name="value">value which need to be format</param>
    /// <param name="args">number of arguments which will replace formatted values</param>
    /// <returns>Formatted string</returns>
    /// <CreatedBy>Naeem Raza</CreatedBy>
    public static string FormatString(this string value, params object[] args)
    {
        Regex regex = new Regex(@"\{.*?\}");
        MatchCollection mc = regex.Matches(value);
        int count = 0;
        foreach (string matchedValue in mc.Select(v => v.Value))
        {
            if (args.Length <= count)
            {
                break;
            }
            if (args[count] != null)
                value = value.Replace(matchedValue, args[count].ToString());
            // If any element of arguments is null then replace with "Null" 
            //string. Null or Empty string is not allowed in uri
            // Add respective handling later on in code.
            else value = value.Replace(matchedValue, System.Net.WebUtility.UrlEncode("Null"));
            count += 1;
        }
        return value;
    }

    /// <summary>
    /// This method used to return standard cache key
    /// </summary>
    /// <param name="typeName">Cache Key Type Name</param>
    /// <param name="args">Cache Unique identifiers</param>
    /// <returns>Cache key</returns>
    /// <CreatedBy>Naeem Raza</CreatedBy>
    public static string FormatCacheKey(this string typeName, params object[] args)
    {

        typeName = "urn:" + typeName;
        foreach (object arg in args)
        {
            typeName = typeName + ":" + arg.ToString();
        }
        return typeName;
    }

    public static string SubstringSafe(this string value, int startIndex, int length)
    {
        if (string.IsNullOrEmpty(value) || value.Length < startIndex)
            return string.Empty;

        return value.Length >= startIndex + length
            ? value.Substring(startIndex, length) : value.Substring(startIndex);
    }

    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...[truncated]";
    }
}
