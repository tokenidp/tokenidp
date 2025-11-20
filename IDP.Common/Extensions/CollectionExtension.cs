namespace IDP.Common.Extensions;

public static class CollectionExtension
{
    public static bool IsSafe<T>(this IEnumerable<T> list)
    {
        return list?.Any() == true;
    }

    public static bool IsSafe<T>(this List<T> list)
    {
        return list?.Any() == true;
    }

    public static bool IsSafe<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        return dictionary?.Any() == true;
    }

    public static bool IsSafe<T>(this T[] arrary)
    {
        return arrary?.Any() == true;
    }
}
