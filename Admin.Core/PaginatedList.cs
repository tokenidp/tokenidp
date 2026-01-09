namespace Admin.Core;

internal class PaginatedList<T>
{
    public List<T> Items { get; private set; }
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public int CurrentPage { get; private set; }
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        TotalCount = count;
        Items = items;
        TotalPages = (int)Math.Ceiling(TotalCount / (float)pageSize);
        CurrentPage = pageIndex;
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageIndex,
        int pageSize,
        bool searchAll)
    {
        var count = await source.CountAsync();

        List<T> items;

        if (!searchAll)
        {
            items = await source.Skip((pageIndex - 1) * pageSize)
                .Take(pageSize).ToListAsync();
        }
        else
        {
            items = await source.ToListAsync();
        }

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
