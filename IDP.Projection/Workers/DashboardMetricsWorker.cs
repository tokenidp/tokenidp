using IDP.Projection.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace IDP.Projection.Workers;

public sealed class DashboardMetricsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger<DashboardMetricsWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:Dashboard:{Guid.NewGuid():N}";

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    public DashboardMetricsWorker(
        IServiceScopeFactory scopeFactory,
        IAppLogger<DashboardMetricsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInfo("DashboardMetricsWorker started. WorkerId={WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = Guid.NewGuid().ToString();

            using (ScopeContext.PushProperty("CorrelationId", correlationId))
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
    }

    private async Task ProcessHourlyMetricsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
        ApplicationDbContext db,
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
            tenantId,
            metricKey: "hourly_metrics",
            processedUntil: bucketEnd,
            ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task UpdateCheckpointAsync(
        ApplicationDbContext db,
        int tenantId,
        string metricKey,
        DateTime processedUntil,
        CancellationToken ct)
    {
        var checkpoint = await db.DashboardMetricsCheckpoints
            .SingleOrDefaultAsync(
                c => c.TenantId == tenantId && c.MetricKey == metricKey,
                ct);

        if (checkpoint == null)
        {
            checkpoint = DashboardMetricsCheckpoint.Create(tenantId,
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
