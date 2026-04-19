using TokenIDP.Core.Abstractions.Queries;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Core.Admin.Dashboard;
using TokenIDP.Domain.AggregateRoots;
using TokenIDP.Domain.AggregateRoots.Configurations;
using TokenIDP.Domain.ReadModels;
using TokenIDP.Domain.ReadModels.Enums;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class DashboardReadService : IDashboardReadService
{
    private const string DashboardRegionConfigKey = "dashboard.region";
    private const string DashboardVersionConfigKey = "dashboard.version";
    private const string DashboardLastKeyRotationConfigKey = "dashboard.last_key_rotation_utc";

    private readonly ApplicationDbContext _db;
    private readonly IClientRepository _clientRepository;
    private readonly ITenantConfigurationRepository _tenantConfigurationRepository;
    private readonly IConfiguration _configuration;

    public DashboardReadService(
        ApplicationDbContext db,
        IClientRepository clientRepository,
        ITenantConfigurationRepository tenantConfigurationRepository,
        IConfiguration configuration)
    {
        _db = db;
        _clientRepository = clientRepository;
        _tenantConfigurationRepository = tenantConfigurationRepository;
        _configuration = configuration;
    }

    public async Task<DashboardResponse> GetDashboardAsync(int tenantId, DashboardPeriod period, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var minute = now.Minute - now.Minute % 15;
        var currentWindowStart = period.GetWindowStart(now);
        var currentBucketStart = period.GetCurrentBucketStart(now);
        var latest15MinBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0, DateTimeKind.Utc);
        var dashboard = new DashboardResponse
        {
            Period = period.ToQueryValue(),
            PeriodLabel = period.ToLabel()
        };

        await PopulateTokenMetricsAsync(dashboard, tenantId, period, currentWindowStart, currentBucketStart, now, ct);
        await PopulateAuthMetricsAsync(dashboard, tenantId, period, currentWindowStart, currentBucketStart, now, ct);
        await PopulateTopClientsAsync(dashboard, tenantId, currentWindowStart, now, ct);
        await PopulateFailedLoginsAsync(dashboard, tenantId, latest15MinBucketStart, ct);
        await PopulateTechnicalDetailsAsync(dashboard, tenantId, now, ct);

        var expiringSecret = await _clientRepository.GetClientExpiringSecretsAsync(7, ct);
        dashboard.ExpiringClientCount = expiringSecret?.ExpiringClientCount ?? 0;
        dashboard.LastUpdated = now;

        return dashboard;
    }

    private async Task PopulateTokenMetricsAsync(
        DashboardResponse dashboard,
        int tenantId,
        DashboardPeriod period,
        DateTime since,
        DateTime currentBucketStart,
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
                m.BucketStart < currentBucketStart)
            .OrderBy(m => m.BucketStart)
            .Select(m => new { m.MetricKey, m.BucketStart, m.MetricValue, m.DimensionKey })
            .ToListAsync(ct);

        var currentHourTokens = await _db.TokenReadModel
            .AsNoTracking()
            .Where(t =>
                t.TenantId == tenantId &&
                t.CreatedOn >= currentBucketStart &&
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
            .GroupBy(x => period.NormalizeSeriesBucketStart(x.BucketStart))
            .Select(g => new TimeSeriesPoint
            {
                Timestamp = g.Key,
                AccessTokens = g.Where(x => x.MetricKey == MetricType.TokensIssued).Sum(x => x.MetricValue),
                RefreshTokens = g.Where(x => x.MetricKey == MetricType.RefreshTokensIssued).Sum(x => x.MetricValue),
                Value = g.Where(x =>
                    x.MetricKey == MetricType.TokensIssued ||
                    x.MetricKey == MetricType.RefreshTokensIssued).Sum(x => x.MetricValue)
            })
            .Append(new TimeSeriesPoint
            {
                Timestamp = currentBucketStart,
                AccessTokens = currentHourTokens.Count(x => x.SourceType == "JWT" || x.SourceType == "Reference"),
                RefreshTokens = currentHourTokens.Count(x => x.SourceType == "Refresh"),
                Value = currentHourTokens.Count(x =>
                    x.SourceType == "JWT" ||
                    x.SourceType == "Reference" ||
                    x.SourceType == "Refresh")
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
        DashboardPeriod period,
        DateTime since,
        DateTime currentBucketStart,
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
                m.BucketStart < currentBucketStart)
            .OrderBy(m => m.BucketStart)
            .Select(m => new { m.MetricKey, m.BucketStart, m.MetricValue })
            .ToListAsync(ct);

        var currentHourActivities = await _db.Activities
            .AsNoTracking()
            .Where(a =>
                a.TenantId == tenantId &&
                a.CreatedAtUtc >= currentBucketStart &&
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
        dashboard.AccountLockout = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.TenantId == tenantId &&
                !u.IsDeleted &&
                u.LockoutEnabled &&
                u.LockoutEnd.HasValue &&
                u.LockoutEnd > now)
            .CountAsync(ct);
        dashboard.TotalLoginAttempts = dashboard.SuccessfulLogins + dashboard.FailedLogins;

        var historicalAuthSeries = historicalRows
            .GroupBy(x => period.NormalizeSeriesBucketStart(x.BucketStart))
            .Select(g => new TimeSeriesPoint
            {
                Timestamp = g.Key,
                Successful = g.Where(x => x.MetricKey == MetricType.AuthSuccess).Sum(x => x.MetricValue),
                Failed = g.Where(x => x.MetricKey == MetricType.AuthFailed).Sum(x => x.MetricValue),
                MfaChallenges = g.Where(x => x.MetricKey == MetricType.MfaChallenges).Sum(x => x.MetricValue),
                AccountLockouts = g.Where(x => x.MetricKey == MetricType.AccountLockout).Sum(x => x.MetricValue),
                Value = g.Where(x => x.MetricKey == MetricType.AuthSuccess || x.MetricKey == MetricType.AuthFailed)
                    .Sum(x => x.MetricValue)
            });

        dashboard.AuthLast24h = historicalAuthSeries
            .Append(new TimeSeriesPoint
            {
                Timestamp = currentBucketStart,
                Successful = currentHourActivities.Count(x => x == ActivityEventType.LoginSucceeded),
                Failed = currentHourActivities.Count(x => x == ActivityEventType.LoginFailed),
                MfaChallenges = currentHourActivities.Count(x => x == ActivityEventType.MfaChallengeSent),
                AccountLockouts = currentHourActivities.Count(x => x == ActivityEventType.AccountLocked),
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

    private async Task PopulateTechnicalDetailsAsync(
        DashboardResponse dashboard,
        int tenantId,
        DateTime now,
        CancellationToken ct)
    {
        dashboard.ActiveSessions = await _db.TokenReadModel
            .AsNoTracking()
            .Where(t =>
                t.TenantId == tenantId &&
                (t.SourceType == "JWT" || t.SourceType == "Reference") &&
                t.ExpiresAt > now &&
                !string.Equals(t.Status, "Revoked", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(t.Status, "Expired", StringComparison.OrdinalIgnoreCase))
            .CountAsync(ct);

        var activeClientsQuery = _db.Clients
            .AsNoTracking()
            .Where(c =>
                c.TenantId == tenantId &&
                c.IsActive &&
                !c.IsDeleted);

        dashboard.RegisteredClients = await activeClientsQuery.CountAsync(ct);

        var averageTokenLifetimeMinutes = await activeClientsQuery
            .Select(c => (double?)c.AccessTokenLifetime)
            .AverageAsync(ct);

        dashboard.AverageTokenTtlSeconds = averageTokenLifetimeMinutes.HasValue
            ? Convert.ToInt32(Math.Round(averageTokenLifetimeMinutes.Value * 60, 0))
            : 0;

        var processStartUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();
        dashboard.UptimeSeconds = Math.Max(0, (long)(now - processStartUtc).TotalSeconds);

        dashboard.Region = await ResolveTenantSettingAsync(
                tenantId,
                DashboardRegionConfigKey,
                ConfigurationScopes.System,
                ct)
            ?? _configuration["Dashboard:Region"]
            ?? _configuration["Region"]
            ?? Environment.GetEnvironmentVariable("WEBSITE_REGION")
            ?? Environment.GetEnvironmentVariable("REGION_NAME")
            ?? string.Empty;

        dashboard.Version = await ResolveTenantSettingAsync(
                tenantId,
                DashboardVersionConfigKey,
                ConfigurationScopes.System,
                ct)
            ?? _configuration["Dashboard:Version"]
            ?? GetApplicationVersion();

        var lastRotationValue = await ResolveTenantSettingAsync(
            tenantId,
            DashboardLastKeyRotationConfigKey,
            ConfigurationScopes.Security,
            ct);

        dashboard.LastKeyRotationUtc = ParseUtcDateTime(lastRotationValue);
    }

    private async Task<string?> ResolveTenantSettingAsync(
        int tenantId,
        string key,
        ConfigurationScopes scope,
        CancellationToken ct)
    {
        var config = await _tenantConfigurationRepository.GetByKeyAsync(
            tenantId,
            key,
            scope,
            cancellationToken: ct);

        return string.IsNullOrWhiteSpace(config?.ConfigValue)
            ? null
            : config.ConfigValue.Trim();
    }

    private static DateTime? ParseUtcDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        var version = assembly.GetName().Version;
        return version is null ? string.Empty : $"v{version}";
    }
}
