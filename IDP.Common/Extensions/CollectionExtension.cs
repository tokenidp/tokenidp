namespace IDP.Common.Extensions;

public static class CollectionExtension
{
    public static bool IsSafe<T>(this IEnumerable<T>? source)
        => source switch
        {
            null => false,
            ICollection<T> c => c.Count > 0,
            IReadOnlyCollection<T> c => c.Count > 0,
            _ => source.Any()
        };

    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
        => source ?? Enumerable.Empty<T>();

    public static T[] OrEmptyArray<T>(this IEnumerable<T>? source)
        => source?.ToArray() ?? Array.Empty<T>();
}
