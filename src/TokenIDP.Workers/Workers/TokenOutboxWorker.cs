using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using TokenIDP.Core.Abstractions;
using TokenIDP.Workers.HealthChecks.States;
using TokenIDP.Workers.Mappers;
using TokenIDP.Workers.Projectors;
using TokenIDP.Workers.Queries;

namespace TokenIDP.Workers.Workers;

internal sealed class TokenOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger<TokenOutboxWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:Token:{Guid.NewGuid():N}";

    private const int BatchSize = 100;
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);
    private const int MaxRetries = 5;
    private static readonly TimeSpan InitialIdleDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxIdleDelay = TimeSpan.FromMinutes(1);
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
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInfo("TokenOutboxWorker started. WorkerId={WorkerId}", _workerId);

        var idleDelay = InitialIdleDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = Guid.NewGuid().ToString();

            using (ScopeContext.PushProperty("CorrelationId", correlationId))
            {
                try
                {
                    _state.Heartbeat();

                    var claimedWork = await ExecuteCycleAsync(stoppingToken, idleDelay);
                    idleDelay = claimedWork
                        ? InitialIdleDelay
                        : GetNextIdleDelay(idleDelay);
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
    }

    private async Task<bool> ExecuteCycleAsync(CancellationToken ct, TimeSpan idleDelay)
    {
        var claimedIds = await ClaimBatchAsync(ct);

        if (claimedIds.Count == 0)
        {
            await SafeDelay(idleDelay, ct);
            return false;
        }

        await ProcessBatchAsync(claimedIds, ct);
        return true;
    }

    private async Task<List<long>> ClaimBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await OutboxBatchClaimer.ClaimBatchAsync(
            db,
            OutboxConsumers.TokenReadModel,
            BatchSize,
            LockDuration,
            _workerId,
            ct);
    }

    private async Task ProcessBatchAsync(List<long> ids, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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

    private static TimeSpan GetNextIdleDelay(TimeSpan currentDelay)
    {
        var nextSeconds = Math.Min(
            MaxIdleDelay.TotalSeconds,
            Math.Max(InitialIdleDelay.TotalSeconds, currentDelay.TotalSeconds * 2));

        return TimeSpan.FromSeconds(nextSeconds);
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

