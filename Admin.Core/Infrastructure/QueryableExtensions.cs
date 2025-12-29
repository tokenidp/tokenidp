using System.Linq.Expressions;

namespace Admin.Core.Infrastructure;

public static class QueryableExtensions
{
    public static Task<PaginatedList<TDestination>> PaginatedTo<TDestination>(
        this IQueryable<TDestination> queryable,
        int pageNumber,
        int pageSize,
        bool searchAll)
    {
        return PaginatedList<TDestination>.CreateAsync(queryable, pageNumber, pageSize, searchAll);
    }

    public static IQueryable<T> ApplySort<T>(
       this IQueryable<T> query,
       string column,
       string order)
    {
        if (string.IsNullOrWhiteSpace(column))
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.PropertyOrField(parameter, column);

        var lambda = Expression.Lambda(property, parameter);

        string methodName = order?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var call = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), property.Type },
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(call);
    }

    public static IQueryable<T> ApplyFilter<T>(
    this IQueryable<T> query,
    IEnumerable<SearchCriteria> criteria)
    {
        if (criteria == null || !criteria.Any())
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var c in criteria)
        {
            var member = Expression.PropertyOrField(parameter, c.ColumnName);
            Expression condition;

            switch (c.ColumnType)
            {
                case SearchColumnType.String:
                    var containsMethod = typeof(string)
                        .GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
                    condition = Expression.Call(
                        member,
                        containsMethod,
                        Expression.Constant(c.Value));
                    break;

                case SearchColumnType.Date:
                    var dateValue = DateTime.Parse(c.Value);
                    condition = Expression.Equal(
                        member,
                        Expression.Constant(dateValue));
                    break;

                case SearchColumnType.Integer:
                    condition = Expression.Equal(
                        member,
                        Expression.Constant(int.Parse(c.Value)));
                    break;

                case SearchColumnType.Decimal:
                    condition = Expression.Equal(
                        member,
                        Expression.Constant(decimal.Parse(c.Value)));
                    break;

                default:
                    continue;
            }

            combined = combined == null
                ? condition
                : Expression.AndAlso(combined, condition);
        }

        if (combined == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);
        return query.Where(lambda);
    }
}
