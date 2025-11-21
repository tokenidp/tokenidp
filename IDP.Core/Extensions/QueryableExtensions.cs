using System.Linq.Expressions;

namespace IDP.Core.Extensions;

//https://www.devtrends.co.uk/blog/stop-using-automapper-in-your-data-access-code

public static class QueryableExtensions
{
    public static ProjectionExpression<TSource> Project<TSource>(this IQueryable<TSource> source)
    {
        return new ProjectionExpression<TSource>(source);
    }
}

public static class ExpressionCache
{
    private static readonly Dictionary<string, Expression> _expressionCache = new();

    public static bool ContainsKey(string key)
    {
        return _expressionCache.ContainsKey(key);
    }

    public static Expression GetExpression(string key)
    {
        return _expressionCache.TryGetValue(key, out var expression) ? expression : null;
    }

    // Method to add or update an expression in the cache
    public static void AddOrUpdateExpression(string key, Expression expression)
    {
        _expressionCache.Add(key, expression);
    }
}

public class ProjectionExpression<TSource>
{
    private readonly IQueryable<TSource> _source;

    public ProjectionExpression(IQueryable<TSource> source)
    {
        _source = source;
    }

    public IQueryable<TDest> To<TDest>()
    {
        var queryExpression = GetCachedExpression<TDest>() ?? BuildExpression<TDest>();

        return _source.Select(queryExpression);
    }

    private static Expression<Func<TSource, TDest>> GetCachedExpression<TDest>()
    {
        var key = GetCacheKey<TDest>();

        return ExpressionCache.ContainsKey(key)
            ? ExpressionCache.GetExpression(key) as Expression<Func<TSource, TDest>> : null;
    }

    private static Expression<Func<TSource, TDest>> BuildExpression<TDest>()
    {
        var sourceProperties = typeof(TSource).GetProperties();
        var destinationProperties = typeof(TDest).GetProperties().Where(dest => dest.CanWrite);
        var parameterExpression = Expression.Parameter(typeof(TSource), "src");

        var bindings = destinationProperties
                            .Select(destinationProperty => BuildBinding(parameterExpression, destinationProperty, sourceProperties))
                            .Where(binding => binding != null);

        var expression = Expression
            .Lambda<Func<TSource, TDest>>(Expression.MemberInit(Expression.New(typeof(TDest)), bindings), parameterExpression);

        var key = GetCacheKey<TDest>();

        ExpressionCache.AddOrUpdateExpression(key, expression);

        return expression;
    }

    private static MemberAssignment BuildBinding(Expression parameterExpression,
        MemberInfo destinationProperty,
        IEnumerable<PropertyInfo> sourceProperties)
    {
        var sourceProperty = sourceProperties.FirstOrDefault(src => src.Name == destinationProperty.Name);

        if (sourceProperty != null)
        {
            return Expression.Bind(destinationProperty, Expression.Property(parameterExpression, sourceProperty));
        }

        var propertyNames = SplitCamelCase(destinationProperty.Name);

        if (propertyNames.Length == 2)
        {
            sourceProperty = sourceProperties.FirstOrDefault(src => src.Name == propertyNames[0]);

            if (sourceProperty != null)
            {
                var sourceChildProperty = sourceProperty.PropertyType.GetProperties().First(src => src.Name == propertyNames[1]);

                if (sourceChildProperty != null)
                {
                    return Expression.Bind(destinationProperty,
                        Expression.Property(Expression.Property(parameterExpression, sourceProperty), sourceChildProperty));
                }
            }
        }

        return null;
    }

    private static string GetCacheKey<TDest>()
    {
        return string.Concat(typeof(TSource).FullName, typeof(TDest).FullName);
    }

    private static string[] SplitCamelCase(string input)
    {
        return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim().Split(' ');
    }
}
