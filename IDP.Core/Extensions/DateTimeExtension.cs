using System.Globalization;

namespace IDP.Core.Extensions;

public static class DateTimeExtension
{
    /// <summary>
    /// Convert date string to RFC format
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static DateTime? ConvertDateToSortable(this DateTime? value)
    {
        if (!value.HasValue)
        {
            return value;
        }

        value = DateTime.Parse(
            value.Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"),
            CultureInfo.InvariantCulture
            );

        return value;
    }
}
