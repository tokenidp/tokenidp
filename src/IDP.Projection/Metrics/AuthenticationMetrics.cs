using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Metrics;

public sealed class AuthenticationMetrics : IDashboardMetricCalculator
{
    public async Task CalculateAsync(
        ApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct)
    {
        var counts = await db.Activities
            .Where(a =>
                a.TenantId == tenantId &&
                a.CreatedAtUtc >= bucketStart &&
                a.CreatedAtUtc < bucketEnd &&
                (a.EventType == ActivityEventType.LoginSucceeded ||
                 a.EventType == ActivityEventType.LoginFailed ||
                 a.EventType == ActivityEventType.MfaChallengeSent ||
                 a.EventType == ActivityEventType.AccountLocked))
            .GroupBy(a => a.EventType)
            .Select(g => new
            {
                EventType = g.Key,
                Count = g.Count()
            }).ToListAsync(ct);

        var successCount = counts.FirstOrDefault(x => x.EventType == ActivityEventType.LoginSucceeded)?.Count ?? 0;
        var failedCount = counts.FirstOrDefault(x => x.EventType == ActivityEventType.LoginFailed)?.Count ?? 0;
        var mfaChallengeCount = counts.FirstOrDefault(x => x.EventType == ActivityEventType.MfaChallengeSent)?.Count ?? 0;
        var accountLockoutCount = counts.FirstOrDefault(x => x.EventType == ActivityEventType.AccountLocked)?.Count ?? 0;

        await DashboardMetricWriter.UpsertAsync(
            db,
            tenantId,
            MetricType.AuthSuccess,
            TimeBucketType.Hour,
            bucketStart,
            bucketEnd,
            successCount,
            MetricDimension.None(),
            ct);

        await DashboardMetricWriter.UpsertAsync(
            db,
            tenantId,
            MetricType.AuthFailed,
            TimeBucketType.Hour,
            bucketStart,
            bucketEnd,
            failedCount,
            MetricDimension.None(),
            ct);

        await DashboardMetricWriter.UpsertAsync(
           db,
           tenantId,
           MetricType.MfaChallenges,
           TimeBucketType.Hour,
           bucketStart,
           bucketEnd,
           mfaChallengeCount,
           MetricDimension.None(),
           ct);

        await DashboardMetricWriter.UpsertAsync(
          db,
          tenantId,
          MetricType.AccountLockout,
          TimeBucketType.Hour,
          bucketStart,
          bucketEnd,
          accountLockoutCount,
          MetricDimension.None(),
          ct);
    }
}