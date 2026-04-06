namespace TokenIDP.Core.Admin.Common;

public static class QueryablePaginationExtensions
{
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        int pageIndex,
        int pageSize,
        bool searchAll)
    {
        var count = await source.CountAsync();

        var items = searchAll
            ? await source.ToListAsync()
            : await source.Skip((pageIndex - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}

