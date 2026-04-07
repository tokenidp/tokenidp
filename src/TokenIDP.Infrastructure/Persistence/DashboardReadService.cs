using TokenIDP.Core.Abstractions.Queries;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Dashboard;
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
        var currentHourBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var latest15MinBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0, DateTimeKind.Utc);
        var dashboard = new DashboardResponse();

        await PopulateTokenMetricsAsync(dashboard, tenantId, currentHourBucketStart, ct);
        await PopulateAuthMetricsAsync(dashboard, tenantId, currentHourBucketStart, ct);
        await PopulateTopClientsAsync(dashboard, tenantId, currentHourBucketStart, ct);
        await PopulateFailedLoginsAsync(dashboard, tenantId, latest15MinBucketStart, ct);

        var expiringSecret = await _clientRepository.GetClientExpiringSecretsAsync(7, ct);
        dashboard.ExpiringClientCount = expiringSecret?.ExpiringClientCount ?? 0;
        dashboard.LastUpdated = now;

        return dashboard;
    }

    private async Task PopulateTokenMetricsAsync(DashboardResponse dashboard, int tenantId, DateTime currentHourBucketStart, CancellationToken ct)
    {
        var since = currentHourBucketStart.AddHours(-24);
        var rows = await _db.DashboardMetrics
            .Where(m =>
                m.TenantId == tenantId &&
                (m.MetricKey == MetricType.TokensIssued
                 || m.MetricKey == MetricType.RefreshTokensIssued
                 || m.MetricKey == MetricType.TokensIssuedPerGrant
                 || m.MetricKey == MetricType.TokensIssuedPerClient) &&
                m.BucketType == TimeBucketType.Hour &&
                m.BucketStart >= since)
            .OrderBy(m => m.BucketStart)
            .Select(m => new { m.MetricKey, m.BucketStart, m.MetricValue, m.DimensionKey })
            .ToListAsync(ct);

        var accessTokenCount = rows.Where(x => x.MetricKey == MetricType.TokensIssued).Sum(x => x.MetricValue);
        var refreshTokenCount = rows.Where(x => x.MetricKey == MetricType.RefreshTokensIssued).Sum(x => x.MetricValue);
        var grantTypeTokens = rows
            .Where(x => x.MetricKey == MetricType.TokensIssuedPerGrant &&
                        x.DimensionKey == MetricDimension.GrantType("authorization_code"))
            .Sum(x => x.MetricValue);

        dashboard.AccessTokenIssued = accessTokenCount;
        dashboard.RefreshTokenIssued = refreshTokenCount;
        dashboard.TokenIssueanceByGrantType = accessTokenCount == 0
            ? 0
            : Convert.ToInt32(Math.Round((double)grantTypeTokens * 100 / accessTokenCount, 0));
        dashboard.TokensLast24h = rows
            .Where(x => x.MetricKey == MetricType.TokensIssued)
            .Select(x => new TimeSeriesPoint { Timestamp = x.BucketStart, Value = x.MetricValue })
            .ToList();

        var maxRow = rows
            .Where(x => x.MetricKey == MetricType.TokensIssuedPerClient && x.BucketStart == currentHourBucketStart)
            .OrderByDescending(x => x.MetricValue)
            .FirstOrDefault();

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

    private async Task PopulateAuthMetricsAsync(DashboardResponse dashboard, int tenantId, DateTime currentHourBucketStart, CancellationToken ct)
    {
        var since = currentHourBucketStart.AddHours(-24);
        var rows = await _db.DashboardMetrics
            .Where(m =>
                m.TenantId == tenantId &&
                (m.MetricKey == MetricType.AuthSuccess
                 || m.MetricKey == MetricType.AuthFailed
                 || m.MetricKey == MetricType.MfaChallenges
                 || m.MetricKey == MetricType.AccountLockout) &&
                m.BucketType == TimeBucketType.Hour &&
                m.BucketStart >= since)
            .OrderBy(m => m.BucketStart)
            .Select(m => new { m.MetricKey, m.BucketStart, m.MetricValue })
            .ToListAsync(ct);

        dashboard.SuccessfulLogins = rows.Where(x => x.MetricKey == MetricType.AuthSuccess).Sum(x => x.MetricValue);
        dashboard.FailedLogins = rows.Where(x => x.MetricKey == MetricType.AuthFailed).Sum(x => x.MetricValue);
        dashboard.MfaChallenge = rows.Where(x => x.MetricKey == MetricType.MfaChallenges).Sum(x => x.MetricValue);
        dashboard.AccountLockout = rows.Where(x => x.MetricKey == MetricType.AccountLockout).Sum(x => x.MetricValue);
        dashboard.TotalLoginAttempts = dashboard.SuccessfulLogins + dashboard.FailedLogins;
        dashboard.AuthLast24h = rows
            .Where(x => x.MetricKey == MetricType.AuthSuccess)
            .Select(x => new TimeSeriesPoint { Timestamp = x.BucketStart, Value = x.MetricValue })
            .ToList();
    }

    private async Task PopulateTopClientsAsync(DashboardResponse dashboard, int tenantId, DateTime currentHourBucketStart, CancellationToken ct)
    {
        var rows = await _db.DashboardMetricRankings
            .Where(r =>
                r.TenantId == tenantId &&
                r.MetricKey == MetricType.TokensIssued &&
                r.BucketType == TimeBucketType.Hour &&
                r.BucketStart == currentHourBucketStart)
            .OrderBy(r => r.Rank)
            .Select(r => new { r.Rank, r.DimensionKey, r.MetricValue })
            .ToListAsync(ct);

        var clientIds = rows
            .Select(r =>
            {
                var clientPart = r.DimensionKey.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(p => p.StartsWith("client:"));
                return clientPart != null && int.TryParse(clientPart["client:".Length..], out var clientId)
                    ? clientId
                    : 0;
            })
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var clients = await Task.WhenAll(clientIds.Select(id => _clientRepository.GetClientShortInfo(id)));
        var clientMap = clients.ToDictionary(c => c.Id, c => c);

        dashboard.TopClients = rows.Select(r =>
        {
            var parts = r.DimensionKey.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var clientPart = parts.FirstOrDefault(p => p.StartsWith("client:"));
            var grantPart = parts.FirstOrDefault(p => p.StartsWith("grant:"));
            var clientId = clientPart != null && int.TryParse(clientPart["client:".Length..], out var parsedClientId)
                ? parsedClientId
                : 0;

            clientMap.TryGetValue(clientId, out var client);

            return new RankingItem
            {
                Rank = r.Rank,
                ClientId = clientId,
                ClientName = client?.ClientName ?? string.Empty,
                GrantType = grantPart != null ? grantPart["grant:".Length..] : string.Empty,
                Tokens = r.MetricValue
            };
        }).ToList();
    }

    private async Task PopulateFailedLoginsAsync(DashboardResponse dashboard, int tenantId, DateTime latest15MinBucketStart, CancellationToken ct)
    {
        dashboard.MultipleFailedLogin = await _db.DashboardMetrics
            .Where(m =>
                m.TenantId == tenantId &&
                m.MetricKey == MetricType.MultipleFailedAttempts &&
                m.BucketType == TimeBucketType.Window15Min &&
                m.BucketStart == latest15MinBucketStart &&
                m.MetricValue >= 100)
            .Select(m => m.MetricValue)
            .FirstOrDefaultAsync(ct);
    }
}
