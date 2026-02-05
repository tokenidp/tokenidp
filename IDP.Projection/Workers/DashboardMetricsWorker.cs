using IDP.Projection.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IDP.Projection.Workers;

public sealed class DashboardMetricsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardMetricsWorker> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public DashboardMetricsWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardMetricsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessHourlyMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard metrics worker failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessHourlyMetricsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var now = DateTime.UtcNow;

        var bucketStart = GetHourlyBucketStart(now);
        var bucketEnd = bucketStart.AddHours(1);

        var tenantIds = await db.Tenants
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            await ProcessTenantHourlyMetricsAsync(
                db,
                tenantId,
                bucketStart,
                bucketEnd,
                ct);
        }
    }

    private async Task ProcessTenantHourlyMetricsAsync(
        IApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct)
    {
        IDashboardMetricCalculator[] calculators =
        {
            new TokensIssuedMetric(),
            new TokensIssuedPerClientMetric(),
            new AuthenticationMetrics(),
           
        };

        foreach (var calculator in calculators)
        {
            await calculator.CalculateAsync(
                db,
                tenantId,
                bucketStart,
                bucketEnd,
                ct);
        }

        var now = DateTime.UtcNow;
        var bucketStart15 = Get15MinBucketStart(now);
        var bucketEnd15 = bucketStart15.AddMinutes(15);

        var minutesMetric = new Authentication15MinAsync();

        await minutesMetric.CalculateAsync(db, tenantId, bucketStart15, bucketEnd15, ct);

        var topClientsRanking = new TopClientsByTokenVolumeRanking();

        await topClientsRanking.CalculateAsync(db, tenantId, bucketStart, bucketEnd, ct);

        await UpdateCheckpointAsync(
            db,
            metricKey: "hourly_metrics",
            processedUntil: bucketEnd,
            ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task UpdateCheckpointAsync(
        IApplicationDbContext db,
        string metricKey,
        DateTime processedUntil,
        CancellationToken ct)
    {
        var checkpoint = await db.DashboardMetricsCheckpoints
            .SingleOrDefaultAsync(c => c.MetricKey == metricKey, ct);

        if (checkpoint == null)
        {
            checkpoint = DashboardMetricsCheckpoint.Create(
                metricKey,
                processedUntil);

            db.DashboardMetricsCheckpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.AdvanceTo(processedUntil);
        }
    }

    private static DateTime GetHourlyBucketStart(DateTime utcNow)
    {
        return new DateTime(
           utcNow.Year,
           utcNow.Month,
           utcNow.Day,
           utcNow.Hour,
           0,
           0,
           DateTimeKind.Utc);
    }

    private static DateTime Get15MinBucketStart(DateTime utcNow)
    {
        var minute = utcNow.Minute - (utcNow.Minute % 15);

        return new DateTime(
            utcNow.Year,
            utcNow.Month,
            utcNow.Day,
            utcNow.Hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}
