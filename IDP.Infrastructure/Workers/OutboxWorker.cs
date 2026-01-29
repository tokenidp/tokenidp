using IDP.Infrastructure.Persistence;
using IDP.Infrastructure.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IDP.Infrastructure.Workers;

internal sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger<OutboxWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    private const int BatchSize = 100;
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);
    private const int MaxRetries = 5;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);

    public OutboxWorker(IServiceScopeFactory scopeFactory, IAppLogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo("OutboxWorker started. WorkerId={WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimedIds = await ClaimBatchAsync(stoppingToken);

                if (claimedIds.Count == 0)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                    continue;
                }

                await ProcessBatchAsync(claimedIds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxWorker loop failure");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<List<long>> ClaimBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var ids = await db.Database
            .SqlQueryRaw<long>(
                OutboxSql.ClaimBatch,
                new SqlParameter("@batchSize", BatchSize),
                new SqlParameter("@lockSeconds", (int)LockDuration.TotalSeconds),
                new SqlParameter("@workerId", _workerId)
            )
            .ToListAsync(ct);

        return ids;
    }

    private async Task ProcessBatchAsync(List<long> ids, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var projector = scope.ServiceProvider.GetRequiredService<ITokenReadModelStore>();

        var events = await db.OutboxEvents
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        foreach (var evt in events)
        {
            try
            {
                await projector.ProjectAsync(evt, ct);
                evt.MarkProcessed();
            }
            catch (Exception ex)
            {
                evt.MarkFailed(DateTime.UtcNow, ex.ToString(), ComputeBackoff(evt.RetryCount), MaxRetries);

                _logger.LogError(ex,
                    "Outbox event failed. Id={Id} Type={Type} Retry={Retry}",
                    evt.Id, evt.EventType, evt.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static TimeSpan ComputeBackoff(int retry)
    {
        var seconds = Math.Min(60, Math.Pow(2, retry));
        return TimeSpan.FromSeconds(seconds);
    }
}