using Admin.Core.Tokens.UseCases;
using IDP.Domain.ReadModels;
using IDP.Domain.ReadModels.Enums;
using IDP.Foundation.Abstractions.Stores;

namespace Admin.Core.Dashboard;

internal sealed class DashboardQueryUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _db;
    private readonly IClientStore _clientStore;
    private readonly IAppLogger<TokenCommandUseCase> _logger;
    private DashboardResponse dashboardResponse = default!;

    public DashboardQueryUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TokenCommandUseCase> logger,
        IClientStore clientStore)
    {
        _db = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _clientStore = clientStore;
    }

    public async Task<ApiResult<DashboardResponse>> GetDashboard(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var minute = now.Minute - (now.Minute % 15);
        var currentHourBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var latest15MinBucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0, DateTimeKind.Utc);

        dashboardResponse = new DashboardResponse();

        await GetTokensLast24hAsync(currentHourBucketStart, ct);
        await GetAuthSummaryLast24hAsync(currentHourBucketStart, ct);
        await GetTopClientsCurrentHourAsync(currentHourBucketStart, ct);
        await GetFailedLoginSpikes15MinAsync(latest15MinBucketStart, threshold: 5, ct);
        await GetClientExpiringSecretsAsync(ct);

        dashboardResponse.LastUpdated = now;

        return ApiResult<DashboardResponse>.Success(dashboardResponse);
    }

    private async Task GetTokensLast24hAsync(DateTime currentHourBucketStart,
        CancellationToken ct)
    {
        var since = currentHourBucketStart.AddHours(-24);

        var tokensIssued = await _db.DashboardMetrics
            .Where(m =>
                m.TenantId == _currentUserService.TenantId &&
                (m.MetricKey == MetricType.TokensIssued
                || m.MetricKey == MetricType.RefreshTokensIssued
                || m.MetricKey == MetricType.TokensIssuedPerGrant
                || m.MetricKey == MetricType.TokensIssuedPerClient) &&
                m.BucketType == TimeBucketType.Hour &&
                m.BucketStart >= since)
            .OrderBy(m => m.BucketStart)
            .Select(m => new
            {
                m.MetricKey,
                m.BucketStart,
                m.MetricValue,
                m.DimensionKey
            }).ToListAsync(ct);

        var accessTokenCount = tokensIssued.Where(x => x.MetricKey == MetricType.TokensIssued).Sum(s => s.MetricValue);
        var refreshTokenCount = tokensIssued.Where(x => x.MetricKey == MetricType.RefreshTokensIssued).Sum(s => s.MetricValue);

        var grantTypeTokens = tokensIssued
            .Where(x => x.MetricKey == MetricType.TokensIssuedPerGrant
            && x.DimensionKey == MetricDimension.GrantType("authorization_code"))
            .Sum(s => s.MetricValue);

        var total = accessTokenCount;

        var percentage = total == 0 ? 0 : Math.Round((double)grantTypeTokens * 100 / total, 0);

        var timeSeries = tokensIssued.Where(x => x.MetricKey == MetricType.TokensIssued)
            .Select(s => new TimeSeriesPoint
            {
                Timestamp = s.BucketStart,
                Value = s.MetricValue,
            }).ToList();

        var maxRow = tokensIssued.Where(x => x.MetricKey == MetricType.TokensIssuedPerClient &&
                                x.BucketStart == currentHourBucketStart).OrderByDescending(x => x.MetricValue)
                                .FirstOrDefault();

        dashboardResponse.AccessTokenIssued = accessTokenCount;
        dashboardResponse.RefreshTokenIssued = refreshTokenCount;
        dashboardResponse.TokenIssueanceByGrantType = Convert.ToInt32(percentage);
        dashboardResponse.TokensLast24h = timeSeries;

        if (maxRow != null && maxRow.MetricValue > 1)
        {
            var clientPart = maxRow.DimensionKey!
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(p => p.StartsWith("client:"));

            var id = clientPart != null ? int.Parse(clientPart["client:".Length..]) : 0;

            var client = await _clientStore.GetClientShortInfo(id);

            dashboardResponse.TokenVolumeSpike.Value = maxRow.MetricValue;
            dashboardResponse.TokenVolumeSpike.Dimension = client.ClientName;
        }
    }

    private async Task GetAuthSummaryLast24hAsync(DateTime currentHourBucketStart,
        CancellationToken ct)
    {
        var since = currentHourBucketStart.AddHours(-24);

        var tokensIssued = await _db.DashboardMetrics
            .Where(m =>
                m.TenantId == _currentUserService.TenantId &&
                (m.MetricKey == MetricType.AuthSuccess
                || m.MetricKey == MetricType.AuthFailed
                || m.MetricKey == MetricType.MfaChallenges
                || m.MetricKey == MetricType.AccountLockout) &&
                m.BucketType == TimeBucketType.Hour &&
                m.BucketStart >= since)
            .OrderBy(m => m.BucketStart)
            .Select(m => new
            {
                m.MetricKey,
                m.BucketStart,
                m.MetricValue,
                m.DimensionKey
            }).ToListAsync(ct);

        var totalLoginAttempts = tokensIssued.Where(x => x.MetricKey == MetricType.AuthSuccess
        || x.MetricKey == MetricType.AuthFailed).Sum(s => s.MetricValue);

        var successfulLogins = tokensIssued.Where(x => x.MetricKey == MetricType.AuthSuccess).Sum(s => s.MetricValue);
        var failedLogins = tokensIssued.Where(x => x.MetricKey == MetricType.AuthFailed).Sum(s => s.MetricValue);
        var mfaChallenge = tokensIssued.Where(x => x.MetricKey == MetricType.MfaChallenges).Sum(s => s.MetricValue);
        var lockoutAccounts = tokensIssued.Where(x => x.MetricKey == MetricType.AccountLockout).Sum(s => s.MetricValue);

        var timeSeries = tokensIssued.Where(x => x.MetricKey == MetricType.AuthSuccess)
           .Select(s => new TimeSeriesPoint
           {
               Timestamp = s.BucketStart,
               Value = s.MetricValue,
           }).ToList();

        dashboardResponse.SuccessfulLogins = successfulLogins;
        dashboardResponse.FailedLogins = failedLogins;
        dashboardResponse.MfaChallenge = mfaChallenge;
        dashboardResponse.AccountLockout = lockoutAccounts;
        dashboardResponse.AuthLast24h = timeSeries;
        dashboardResponse.TotalLoginAttempts = successfulLogins + failedLogins;
    }

    private async Task GetTopClientsCurrentHourAsync(DateTime currentHourBucketStart,
        CancellationToken ct)
    {
        var since = currentHourBucketStart.AddHours(-24);

        var rows = await _db.DashboardMetricRankings
            .Where(r =>
                r.TenantId == _currentUserService.TenantId &&
                r.MetricKey == MetricType.TokensIssued &&
                r.BucketType == TimeBucketType.Hour &&
                r.BucketStart == currentHourBucketStart)
            .OrderBy(r => r.Rank)
            .Select(r => new
            {
                r.Rank,
                r.DimensionKey,
                r.MetricValue
            }).ToListAsync(ct);

        var clientIds = rows
            .Select(r =>
            {
                var clientPart = r.DimensionKey
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(p => p.StartsWith("client:"));

                return clientPart != null
                    ? int.Parse(clientPart["client:".Length..])
                    : 0;
            }).Where(id => id > 0)
            .Distinct()
            .ToList();

        var clients = await Task.WhenAll(
            clientIds.Select(id => _clientStore.GetClientShortInfo(id))
        );

        var clientMap = clients.Where(c => c != null)
            .ToDictionary(c => c!.Id, c => c!);

        var result = rows.Select(r =>
        {
            var parts = r.DimensionKey.Split('|', StringSplitOptions.RemoveEmptyEntries);

            var clientPart = parts.FirstOrDefault(p => p.StartsWith("client:"));
            var grantPart = parts.FirstOrDefault(p => p.StartsWith("grant:"));

            var clientId = clientPart != null ? int.Parse(clientPart["client:".Length..]) : 0;
            var grantType = grantPart != null ? grantPart["grant:".Length..] : string.Empty;

            clientMap.TryGetValue(clientId, out var client);

            return new RankingItem
            {
                Rank = r.Rank,
                ClientName = client?.ClientName ?? string.Empty,
                ClientId = clientId,
                GrantType = grantType,
                Tokens = r.MetricValue
            };
        }).ToList();

        dashboardResponse.TopClients = result;
    }

    private async Task GetFailedLoginSpikes15MinAsync(DateTime latest15MinBucketStart,
        long threshold,
        CancellationToken ct)
    {
        var failedLogines = await _db.DashboardMetrics
            .Where(m =>
                m.TenantId == _currentUserService.TenantId &&
                m.MetricKey == MetricType.MultipleFailedAttempts &&
                m.BucketType == TimeBucketType.Window15Min &&
                m.BucketStart == latest15MinBucketStart &&
                m.MetricValue >= threshold)
            .Select(m => new
            {
                m.MetricKey,
                m.MetricValue
            }).FirstOrDefaultAsync(ct);

        dashboardResponse.MultipleFailedLogin = failedLogines?.MetricValue ?? 0;
    }

    private async Task GetClientExpiringSecretsAsync(CancellationToken ct)
    {
        var secret = await _clientStore.GetClientExpiringSecretsAsync(7, ct);

        dashboardResponse.ExpiringClientCount = secret?.ExpiringClientCount ?? 0;
    }
}
