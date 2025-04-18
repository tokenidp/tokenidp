namespace Services.Common.Extensions;

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

    /// <summary>
    ///  Enclosed each value in single quotes and 
    ///  convert list of strings to comma separated string
    ///  For example, "'Hello','Test'"
    /// </summary>
    /// <param name="values">list of strings</param>
    /// <returns>Comma sperated string and enclose each value in single quotes</returns>
    public static string EnclosedInSingleQuotes(this IEnumerable<string> values)
    {
        string value = string.Format("'{0}'", string.Join("','",
            values.Select(i => i.Replace("'", "''"))));

        return value;
    }

    /// <summary>
    /// Format Name
    /// </summary>
    /// <param name="value">Reference Id</param>     
    /// <returns>external id formatter to get external id in composite request</returns>
    public static string FormatName(this string value)
    {
        Regex extraItems = new Regex("[^A-Za-z ]",
           RegexOptions.Compiled | RegexOptions.IgnoreCase);

        value = extraItems.Replace(value, "").Trim();

        Regex middleInitials = new Regex("((Mr)|(Mrs)|(Sir)|(Jr)|(Jnr)|(Ms)|(Dr)|(Sr)|(Snr)|(Miss))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        value = middleInitials.Replace(value, "").Trim();

        Regex middleInitials2 = new Regex(@"\b[A-Z]{1,2}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        value = middleInitials2.Replace(value, "").Trim();

        Regex spaceReplacer = new Regex("(  )+", RegexOptions.Compiled);

        value = spaceReplacer.Replace(value, " ").Trim();

        value = value.Replace(" ", "%");

        return value;
    }

    /// <summary>
    /// Convert date string to RFC format
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertDateToRFCFormat(this string value)
    {
        value = DateTime.Parse(value, CultureInfo.InvariantCulture)
                        .ToUniversalTime()
                        .ToString("yyyy-MM-dd'T'HH:mm:ss.fff");

        return value;
    }
}
