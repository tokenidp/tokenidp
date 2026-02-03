using IDP.Projection.HealthChecks;
using IDP.Projection.Mappers;
using IDP.Projection.Projectors;
using IDP.Projection.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IDP.Projection.Workers;

internal sealed class TokenOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger<TokenOutboxWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:Token:{Guid.NewGuid():N}";

    private const int BatchSize = 100;
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);
    private const int MaxRetries = 5;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
    private readonly TokenWorkerState _state;

    public TokenOutboxWorker(IServiceScopeFactory scopeFactory,
        IAppLogger<TokenOutboxWorker> logger,
        TokenWorkerState state)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _state = state;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo("OutboxWorker started. WorkerId={WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _state.Heartbeat();

                await ExecuteCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInfo("OutboxWorker stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogFatal(ex, "OutboxWorker crashed. Restarting in 5s");
                await SafeDelay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ExecuteCycleAsync(CancellationToken ct)
    {
        var claimedIds = await ClaimBatchAsync(ct);

        if (claimedIds.Count == 0)
        {
            await SafeDelay(IdleDelay, ct);
            return;
        }

        await ProcessBatchAsync(claimedIds, ct);
    }

    private async Task<List<long>> ClaimBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var now = DateTime.UtcNow;

        var ids = await db.Database
            .SqlQueryRaw<long>(
                OutboxSql.ClaimBatch,
                new SqlParameter("@consumer", OutboxConsumers.TokenReadModel),
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
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var projector = scope.ServiceProvider.GetRequiredService<TokenReadModelProjector>();

        var consumerName = OutboxConsumers.TokenReadModel;

        var events = await db.OutboxEventConsumers.Include(e => e.OutboxEvent)
            .Where(x => ids.Contains(x.OutboxEventId)
            && x.ConsumerName == consumerName)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        foreach (var evt in events)
        {
            try
            {
                await projector.ProjectAsync(evt.OutboxEvent, ct);
                evt.MarkProcessed();
            }
            catch (Exception ex)
            {
                evt.MarkFailed(DateTime.UtcNow, ex.ToString(), ComputeBackoff(evt.RetryCount), MaxRetries);

                _logger.LogError(ex,
                    "Outbox event failed. Id={Id} Type={Type} Retry={Retry}",
                    evt.Id, evt.OutboxEvent.EventType, evt.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static TimeSpan ComputeBackoff(int retry)
    {
        var seconds = Math.Min(60, Math.Pow(2, retry));
        return TimeSpan.FromSeconds(seconds);
    }

    private static async Task SafeDelay(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
        }
        catch (OperationCanceledException) { }
    }
}