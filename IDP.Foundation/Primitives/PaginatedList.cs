namespace IDP.Foundation.Primitives;

public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public int CurrentPage { get; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public PaginatedList(
        IReadOnlyList<T> items,
        int totalCount,
        int pageIndex,
        int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = pageIndex;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}

