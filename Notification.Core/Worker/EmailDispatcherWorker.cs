using IDP.Domain.AggregateRoots.Emails;
using IDP.Foundation.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification.Core.Abstractions;
using Notification.Core.Concrete;
using Notification.Core.Primitives;

namespace Notification.Core.Worker;

public sealed class EmailDispatcherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppLogger<EmailDispatcherWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    public int BatchSize { get; init; } = 50;
    public int LockSeconds { get; init; } = 60;
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimedIds = await ClaimBatchAsync(stoppingToken);

                if (claimedIds.Count == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

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

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInfo("EmailDispatcherWorker stopped. WorkerId={WorkerId}", _workerId);
    }

    private async Task<List<long>> ClaimBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var ids = await db.Database
       .SqlQueryRaw<long>(
           EmailSql.ClaimBatch,
           new SqlParameter("@batchSize", BatchSize),
           new SqlParameter("@lockSeconds", LockSeconds),
           new SqlParameter("@workerId", _workerId)
       )
       .ToListAsync(ct);

        return ids;
    }

    private async Task ProcessClaimedAsync(List<long> ids, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var emailConfigurationProvider = scope.ServiceProvider.GetService<IEmailConfigurationProvider>();

        var emails = await db.EmailMessages
            .Where(x => ids.Contains(x.Id))
            .Include("Recipients")
            .Include("Attachments")
            .ToListAsync(ct);

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
        IEmailConfigurationProvider emailConfigurationProvider,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
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
                // Render if TemplateRef/Hybrid without subject/body
                if (email.PayloadMode is EmailPayloadMode.TemplateRef or EmailPayloadMode.Hybrid)
                {
                    // If rendered bodies already exist (Hybrid), you can decide to skip re-render.
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

                var result = await emailNotification.SendAsync(email, ct);

                if (result.Success)
                {
                    email.MarkSent(result.ProviderMessageId ?? string.Empty);
                    _logger.LogInfo("Email sent. Id={Id} Tenant={TenantId}", email.Id, email.TenantId);
                }
                else if (result.PermanentFailure)
                {
                    email.MarkPermanentFailure(result.Error ?? "Permanent failure");
                    _logger.LogWarning("Email permanent failure. Id={Id} Error={Error}", email.Id, result.Error);
                }
                else
                {
                    var next = retrySchedule.ComputeNextAttemptUtc(email.AttemptCount + 1, DateTime.UtcNow);
                    email.MarkTransientFailure(result.Error ?? "Transient failure", next);
                    _logger.LogWarning("Email transient failure. Id={Id} Next={NextAttempt}", email.Id, next);
                }

                // Optional: write attempt log
                // db.Set<EmailDeliveryAttempt>().Add(...)

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // treat unknown exceptions as transient unless you decide otherwise
                var next = retrySchedule.ComputeNextAttemptUtc(email.AttemptCount + 1, DateTime.UtcNow);
                email.MarkTransientFailure(ex.Message, next);

                _logger.LogError(ex, "Email processing exception. Id={Id}", email.Id);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
