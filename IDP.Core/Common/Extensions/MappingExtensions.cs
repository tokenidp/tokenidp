using System.Linq.Dynamic.Core;
using System.Text;

namespace IDP.Core.Common.Extensions;

internal static class MappingExtensions
{
    public static Task<PaginatedList<TDestination>> PaginatedTo<TDestination>(
        this IQueryable<TDestination> queryable,
        int pageNumber,
        int pageSize,
        bool searchAll)
    {
        return PaginatedList<TDestination>.CreateAsync(queryable, pageNumber, pageSize, searchAll);
    }

    public static IQueryable<TDestination> ApplySort<TDestination>(
        this IQueryable<TDestination> queryable,
        string column,
        string order)
    {
        if (!queryable.Any())
            return queryable;

        if (string.IsNullOrWhiteSpace(column))
        {
            return queryable;
        }

        var orderQuery = $"{column} {order} ";

        return queryable.OrderBy(orderQuery);
    }

    public static IQueryable<TDestination> ApplyFilter<TDestination>(
        this IQueryable<TDestination> queryable,
        IEnumerable<SearchCriteria> searchCriterias)
    {
        if (!searchCriterias.IsSafe())
        {
            return queryable;
        }

        StringBuilder whereClause = new();

        foreach (var criteria in searchCriterias)
        {
            bool isEmptyOrNull = whereClause.Length == 0;
            whereClause.Append(!isEmptyOrNull ? " And " : "");

            if (criteria.ColumnType == SearchColumnType.String)
            {
                var value = criteria.Value.Replace(" ", "%").Replace("'", "%").Replace("\"", "%").Trim();

                whereClause.Append($"{criteria.ColumnName} Like  %\"{value}\"%");
            }
            if (criteria.ColumnType == SearchColumnType.Date)
            {
                whereClause.Append($"{criteria.ColumnName} =  \"{Convert.ToDateTime(criteria.Value).ToString("yyyy-MM-dd")}\"");
            }
            if (criteria.ColumnType == SearchColumnType.Decimal
                || criteria.ColumnType == SearchColumnType.Integer)
            {
                whereClause.Append($"{criteria.ColumnName} =  {criteria.Value}");
            }
        }

        return queryable.Where(whereClause.ToString());
    }
}
