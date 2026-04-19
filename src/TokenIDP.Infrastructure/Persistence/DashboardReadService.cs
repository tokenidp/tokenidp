using TokenIDP.Core.Abstractions.Queries;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Dashboard;
using TokenIDP.Domain.AggregateRoots;
using TokenIDP.Domain.ReadModels;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class DashboardReadService : IDashboardReadService
{
    private readonly ApplicationDbContext _db;
    private readonly IClientRepository _clientRepository;

    public DashboardReadService(
        ApplicationDbContext db,
        IClientRepository clientRepository)
    {
        _db = db;
        _clientRepository = clientRepository;
    }

    public async Task<DashboardResponse> GetDashboardAsync(int tenantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var minute = now.Minute - now.Minute % 15;
        var currentWindowStart = now.AddHours(-24);
        var currentHourBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var latest15MinBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0, DateTimeKind.Utc);
        var dashboard = new DashboardResponse();

        await PopulateTokenMetricsAsync(dashboard, tenantId, currentWindowStart, currentHourBucketStart, now, ct);
        await PopulateAuthMetricsAsync(dashboard, tenantId, currentWindowStart, currentHourBucketStart, now, ct);
        await PopulateTopClientsAsync(dashboard, tenantId, currentWindowStart, now, ct);
        await PopulateFailedLoginsAsync(dashboard, tenantId, latest15MinBucketStart, ct);

        var expiringSecret = await _clientRepository.GetClientExpiringSecretsAsync(7, ct);
        dashboard.ExpiringClientCount = expiringSecret?.ExpiringClientCount ?? 0;
        dashboard.LastUpdated = now;

        return dashboard;
    }

    private async Task PopulateTokenMetricsAsync(
        DashboardResponse dashboard,
        int tenantId,
        DateTime since,
        DateTime currentHourBucketStart,
        DateTime now,
        CancellationToken ct)
    {
        var historicalRows = await _db.DashboardMetrics
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                (m.MetricKey == MetricType.TokensIssued
                 || m.MetricKey == MetricType.RefreshTokensIssued
                 || m.MetricKey == MetricType.TokensIssuedPerGrant
                 || m.MetricKey == MetricType.TokensIssuedPerClient) &&
                m.BucketType == TimeBucketType.Hour &&
                m.BucketStart >= since &&
                m.BucketStart < currentHourBucketStart)
            .OrderBy(m => m.BucketStart)
            .Select(m => new { m.MetricKey, m.BucketStart, m.MetricValue, m.DimensionKey })
            .ToListAsync(ct);

        var currentHourTokens = await _db.TokenReadModel
            .AsNoTracking()
            .Where(t =>
                t.TenantId == tenantId &&
                t.CreatedOn >= currentHourBucketStart &&
                t.CreatedOn <= now)
            .Select(t => new { t.SourceType, t.GrantType, t.ClientId })
            .ToListAsync(ct);

        var accessTokenCount =
            historicalRows.Where(x => x.MetricKey == MetricType.TokensIssued).Sum(x => x.MetricValue) +
            currentHourTokens.Count(x => x.SourceType == "JWT" || x.SourceType == "Reference");

        var refreshTokenCount =
            historicalRows.Where(x => x.MetricKey == MetricType.RefreshTokensIssued).Sum(x => x.MetricValue) +
            currentHourTokens.Count(x => x.SourceType == "Refresh");

        var grantTypeTokens = historicalRows
            .Where(x => x.MetricKey == MetricType.TokensIssuedPerGrant &&
                        x.DimensionKey == MetricDimension.GrantType("authorization_code"))
            .Sum(x => x.MetricValue) +
            currentHourTokens.Count(x =>
                (x.SourceType == "JWT" || x.SourceType == "Reference") &&
                string.Equals(x.GrantType, GrantTypes.authorization_code.ToString(), StringComparison.OrdinalIgnoreCase));

        dashboard.AccessTokenIssued = accessTokenCount;
        dashboard.RefreshTokenIssued = refreshTokenCount;
        dashboard.TokenIssueanceByGrantType = accessTokenCount == 0
            ? 0
            : Convert.ToInt32(Math.Round((double)grantTypeTokens * 100 / accessTokenCount, 0));

        dashboard.TokensLast24h = historicalRows
            .Where(x => x.MetricKey == MetricType.TokensIssued)
            .GroupBy(x => x.BucketStart)
            .Select(g => new TimeSeriesPoint
            {
                Timestamp = g.Key,
                Value = g.Sum(x => x.MetricValue)
            })
            .Append(new TimeSeriesPoint
            {
                Timestamp = currentHourBucketStart,
                Value = currentHourTokens.Count(x => x.SourceType == "JWT" || x.SourceType == "Reference")
            })
            .OrderBy(x => x.Timestamp)
            .ToList();

        var maxRow = historicalRows
            .Where(x => x.MetricKey == MetricType.TokensIssuedPerClient)
            .OrderByDescending(x => x.MetricValue)
            .FirstOrDefault();

        var currentHourMax = currentHourTokens
            .Where(x => x.SourceType == "JWT" || x.SourceType == "Reference")
            .GroupBy(x => x.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        if (currentHourMax != null && currentHourMax.Count >= (maxRow?.MetricValue ?? 0))
        {
            var client = await _clientRepository.GetClientShortInfo(currentHourMax.ClientId);
            dashboard.TokenVolumeSpike.Value = currentHourMax.Count;
            dashboard.TokenVolumeSpike.Dimension = client.ClientName;
            return;
        }

        if (maxRow?.MetricValue > 1 && !string.IsNullOrWhiteSpace(maxRow.DimensionKey))
        {
            var clientPart = maxRow.DimensionKey.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(p => p.StartsWith("client:"));

            if (clientPart != null && int.TryParse(clientPart["client:".Length..], out var clientId))
            {
                var client = await _clientRepository.GetClientShortInfo(clientId);
                dashboard.TokenVolumeSpike.Value = maxRow.MetricValue;
                dashboard.TokenVolumeSpike.Dimension = client.ClientName;
            }
        }
    }

    private async Task PopulateAuthMetricsAsync(
        DashboardResponse dashboard,
        int tenantId,
        DateTime since,
        DateTime currentHourBucketStart,
        DateTime now,
        CancellationToken ct)
    {
        var historicalRows = await _db.DashboardMetrics
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                (m.MetricKey == MetricType.AuthSuccess
                 || m.MetricKey == MetricType.AuthFailed
                 || m.MetricKey == MetricType.MfaChallenges
                 || m.MetricKey == MetricType.AccountLockout) &&
                m.BucketType == TimeBucketType.Hour &&
                m.BucketStart >= since &&
                m.BucketStart < currentHourBucketStart)
            .OrderBy(m => m.BucketStart)
            .Select(m => new { m.MetricKey, m.BucketStart, m.MetricValue })
            .ToListAsync(ct);

        var currentHourActivities = await _db.Activities
            .AsNoTracking()
            .Where(a =>
                a.TenantId == tenantId &&
                a.CreatedAtUtc >= currentHourBucketStart &&
                a.CreatedAtUtc <= now &&
                (a.EventType == ActivityEventType.LoginSucceeded ||
                 a.EventType == ActivityEventType.LoginFailed ||
                 a.EventType == ActivityEventType.MfaChallengeSent ||
                 a.EventType == ActivityEventType.AccountLocked))
            .Select(a => a.EventType)
            .ToListAsync(ct);

        dashboard.SuccessfulLogins =
            historicalRows.Where(x => x.MetricKey == MetricType.AuthSuccess).Sum(x => x.MetricValue) +
            currentHourActivities.Count(x => x == ActivityEventType.LoginSucceeded);
        dashboard.FailedLogins =
            historicalRows.Where(x => x.MetricKey == MetricType.AuthFailed).Sum(x => x.MetricValue) +
            currentHourActivities.Count(x => x == ActivityEventType.LoginFailed);
        dashboard.MfaChallenge =
            historicalRows.Where(x => x.MetricKey == MetricType.MfaChallenges).Sum(x => x.MetricValue) +
            currentHourActivities.Count(x => x == ActivityEventType.MfaChallengeSent);
        dashboard.AccountLockout =
            historicalRows.Where(x => x.MetricKey == MetricType.AccountLockout).Sum(x => x.MetricValue) +
            currentHourActivities.Count(x => x == ActivityEventType.AccountLocked);
        dashboard.TotalLoginAttempts = dashboard.SuccessfulLogins + dashboard.FailedLogins;

        var historicalAuthSeries = historicalRows
            .GroupBy(x => x.BucketStart)
            .Select(g => new TimeSeriesPoint
            {
                Timestamp = g.Key,
                Successful = g.Where(x => x.MetricKey == MetricType.AuthSuccess).Sum(x => x.MetricValue),
                Failed = g.Where(x => x.MetricKey == MetricType.AuthFailed).Sum(x => x.MetricValue),
                Value = g.Where(x => x.MetricKey == MetricType.AuthSuccess || x.MetricKey == MetricType.AuthFailed)
                    .Sum(x => x.MetricValue)
            });

        dashboard.AuthLast24h = historicalAuthSeries
            .Append(new TimeSeriesPoint
            {
                Timestamp = currentHourBucketStart,
                Successful = currentHourActivities.Count(x => x == ActivityEventType.LoginSucceeded),
                Failed = currentHourActivities.Count(x => x == ActivityEventType.LoginFailed),
                Value = currentHourActivities.Count(x =>
                    x == ActivityEventType.LoginSucceeded || x == ActivityEventType.LoginFailed)
            })
            .OrderBy(x => x.Timestamp)
            .ToList();
    }

    private async Task PopulateTopClientsAsync(
        DashboardResponse dashboard,
        int tenantId,
        DateTime since,
        DateTime now,
        CancellationToken ct)
    {
        var rows = await _db.TokenReadModel
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId &&
                (r.SourceType == "JWT" || r.SourceType == "Reference") &&
                r.CreatedOn >= since &&
                r.CreatedOn <= now)
            .GroupBy(r => new { r.ClientId, r.GrantType })
            .Select(g => new
            {
                g.Key.ClientId,
                g.Key.GrantType,
                MetricValue = g.Count()
            })
            .OrderByDescending(r => r.MetricValue)
            .ThenBy(r => r.ClientId)
            .Take(10)
            .ToListAsync(ct);

        var clientIds = rows
            .Select(r => r.ClientId)
            .Distinct()
            .ToList();

        var clients = await Task.WhenAll(clientIds.Select(id => _clientRepository.GetClientShortInfo(id)));
        var clientMap = clients.ToDictionary(c => c.Id, c => c);

        dashboard.TopClients = rows.Select((r, index) =>
        {
            clientMap.TryGetValue(r.ClientId, out var client);

            return new RankingItem
            {
                Rank = index + 1,
                ClientId = r.ClientId,
                ClientName = client?.ClientName ?? string.Empty,
                GrantType = r.GrantType,
                Tokens = r.MetricValue
            };
        }).ToList();
    }

    private async Task PopulateFailedLoginsAsync(DashboardResponse dashboard, int tenantId, DateTime latest15MinBucketStart, CancellationToken ct)
    {
        dashboard.MultipleFailedLogin = await _db.DashboardMetrics
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                m.MetricKey == MetricType.MultipleFailedAttempts &&
                m.BucketType == TimeBucketType.Window15Min &&
                m.BucketStart == latest15MinBucketStart)
            .Select(m => m.MetricValue)
            .FirstOrDefaultAsync(ct);
    }
}
