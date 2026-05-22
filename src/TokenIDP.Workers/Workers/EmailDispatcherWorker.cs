using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TokenIDP.Core.Abstractions;
using TokenIDP.Domain.AggregateRoots.Emails;
using TokenIDP.Infrastructure.Emails.Abstractions;
using TokenIDP.Infrastructure.Emails.Concrete;
using TokenIDP.Workers.Queries;
using TokenIDP.Workers.Workers;

namespace TokenIDP.Workers;

public sealed class EmailDispatcherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger<EmailDispatcherWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    public int BatchSize { get; init; } = 50;
    public int LockSeconds { get; init; } = 60;
    public TimeSpan InitialPollInterval { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxPollInterval { get; init; } = TimeSpan.FromMinutes(1);

    public EmailDispatcherWorker(IServiceScopeFactory scopeFactory,
        IAppLogger<EmailDispatcherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInfo("EmailDispatcherWorker started. WorkerId={WorkerId}", _workerId);

        var pollInterval = InitialPollInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimedIds = await ClaimBatchAsync(stoppingToken);

                if (claimedIds.Count == 0)
                {
                    await Task.Delay(pollInterval, stoppingToken);
                    pollInterval = GetNextPollInterval(pollInterval);
                    continue;
                }

                pollInterval = InitialPollInterval;
                await ProcessClaimedAsync(claimedIds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInfo("OutboxWorker stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailDispatcherWorker loop error");
                await RecordWorkerFailureAsync("EmailDispatcherWorker", ex, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInfo("EmailDispatcherWorker stopped. WorkerId={WorkerId}", _workerId);
    }

    private async Task<List<long>> ClaimBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await EmailBatchClaimer.ClaimBatchAsync(
            db,
            BatchSize,
            TimeSpan.FromSeconds(LockSeconds),
            _workerId,
            ct);
    }

    private async Task ProcessClaimedAsync(List<long> ids, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailConfigurationProvider = scope.ServiceProvider.GetService<EmailConfigurationProvider>();

        var emails = await db.EmailMessages.Where(x => ids.Contains(x.Id)).ToListAsync(ct);

        if (emails == null || emails.Count == 0)
        {
            return;
        }

        IEnumerable<int> tenantIds = emails.Select(s => s.TenantId).Distinct().ToList();

        foreach (var tenantId in tenantIds)
        {
            var tenantEmails = emails.Where(s => s.TenantId == tenantId).ToList();

            await emailConfigurationProvider!.PopulateEmailSettings(tenantId);

            await TenantProcessClaimedAsync(tenantEmails, emailConfigurationProvider, ct);
        }
    }

    private async Task TenantProcessClaimedAsync(List<EmailMessage> emails,
        EmailConfigurationProvider emailConfigurationProvider,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<EmailProviderFactory>();
        var retrySchedule = scope.ServiceProvider.GetRequiredService<IRetrySchedule>();
        var renderer = scope.ServiceProvider.GetService<IEmailTemplateRenderer>();

        var emailNotification = factory.GetService(emailConfigurationProvider.EmailProviderType);

        foreach (var email in emails)
        {
            if (email.Status != EmailStatus.Claimed || email.LockedBy != _workerId)
                continue;

            var attemptStarted = DateTime.UtcNow;

            try
            {
                if (email.PayloadMode is EmailPayloadMode.TemplateRef or EmailPayloadMode.Hybrid)
                {
                    if (string.IsNullOrWhiteSpace(email.Subject) && !string.IsNullOrWhiteSpace(email.TemplateKey))
                    {
                        var (subject, html, text) = await renderer!.RenderAsync(
                            email.TenantId,
                            email.TemplateKey!,
                            email.TemplateModelJson,
                            ct);

                        email.ApplyRenderedBodies(subject, html, text);
                    }
                }

                var attempt = EmailDeliveryAttempt.Start(emailMessageId: email.Id,
                    attemptNo: email.AttemptCount + 1,
                    provider: email.Provider,
                    nowUtc: DateTime.UtcNow);

                var result = await emailNotification.SendAsync(emailConfigurationProvider, email, ct);

                if (result.Success)
                {
                    email.MarkSent(result.ProviderMessageId ?? string.Empty);
                    attempt.MarkSuccess(result.ProviderMessageId, DateTime.UtcNow);
                    _logger.LogInfo("Email sent. Id={Id} Tenant={TenantId}", email.Id, email.TenantId);
                }
                else if (result.PermanentFailure)
                {
                    email.MarkPermanentFailure(result.Error ?? "Permanent failure");
                    attempt.MarkPermanentFailure(result.Error!, DateTime.UtcNow);
                    _logger.LogWarning("Email permanent failure. Id={Id} Error={Error}", email.Id, result.Error!);
                }
                else
                {
                    var next = retrySchedule.ComputeNextAttemptUtc(email.AttemptCount + 1, DateTime.UtcNow);
                    email.MarkTransientFailure(result.Error ?? "Transient failure", next);
                    attempt.MarkTransientFailure(result.Error!, DateTime.UtcNow);
                    _logger.LogWarning("Email transient failure. Id={Id} Next={NextAttempt}", email.Id, next);
                }

                db.EmailDeliveryAttempts.Add(attempt);

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // treat unknown exceptions as transient unless you decide otherwise
                var next = retrySchedule.ComputeNextAttemptUtc(email.AttemptCount + 1, DateTime.UtcNow);
                email.MarkTransientFailure(ex.Message, next);
                BackgroundJobActivityEvents.RaiseFailure(
                    db,
                    "EmailDispatcherWorker",
                    _workerId,
                    ex,
                    tenantId: email.TenantId,
                    targetId: email.Id.ToString());

                _logger.LogError(ex, "Email processing exception. Id={Id}", email.Id);
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task RecordWorkerFailureAsync(
        string jobName,
        Exception exception,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        BackgroundJobActivityEvents.RaiseFailure(db, jobName, _workerId, exception);
        await db.SaveChangesAsync(ct);
    }

    private TimeSpan GetNextPollInterval(TimeSpan currentInterval)
    {
        var nextSeconds = Math.Min(
            MaxPollInterval.TotalSeconds,
            Math.Max(InitialPollInterval.TotalSeconds, currentInterval.TotalSeconds * 2));

        return TimeSpan.FromSeconds(nextSeconds);
    }
}

