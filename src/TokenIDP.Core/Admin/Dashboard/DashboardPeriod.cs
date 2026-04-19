namespace TokenIDP.Core.Admin.Dashboard;

public enum DashboardPeriod
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

public static class DashboardPeriodExtensions
{
    public static DashboardPeriod Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DashboardPeriod.Daily;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "daily" => DashboardPeriod.Daily,
            "weekly" => DashboardPeriod.Weekly,
            "monthly" => DashboardPeriod.Monthly,
            _ => DashboardPeriod.Daily
        };
    }

    public static string ToQueryValue(this DashboardPeriod period) => period switch
    {
        DashboardPeriod.Weekly => "weekly",
        DashboardPeriod.Monthly => "monthly",
        _ => "daily"
    };

    public static string ToLabel(this DashboardPeriod period) => period switch
    {
        DashboardPeriod.Weekly => "Last 7 Days",
        DashboardPeriod.Monthly => "Last 30 Days",
        _ => "Last 24 Hours"
    };

    public static DateTime GetWindowStart(this DashboardPeriod period, DateTime utcNow) => period switch
    {
        DashboardPeriod.Weekly => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-6),
        DashboardPeriod.Monthly => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-29),
        _ => utcNow.AddHours(-24)
    };

    public static DateTime GetCurrentBucketStart(this DashboardPeriod period, DateTime utcNow) => period switch
    {
        DashboardPeriod.Weekly => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc),
        DashboardPeriod.Monthly => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc),
        _ => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc)
    };

    public static DateTime NormalizeSeriesBucketStart(this DashboardPeriod period, DateTime bucketStart) => period switch
    {
        DashboardPeriod.Weekly => new DateTime(bucketStart.Year, bucketStart.Month, bucketStart.Day, 0, 0, 0, DateTimeKind.Utc),
        DashboardPeriod.Monthly => new DateTime(bucketStart.Year, bucketStart.Month, bucketStart.Day, 0, 0, 0, DateTimeKind.Utc),
        _ => new DateTime(bucketStart.Year, bucketStart.Month, bucketStart.Day, bucketStart.Hour, 0, 0, DateTimeKind.Utc)
    };
}
