namespace TokenIDP.Core.Admin.Dashboard;

public class DashboardResponse
{
    public string Period { get; set; } = DashboardPeriod.Daily.ToQueryValue();
    public string PeriodLabel { get; set; } = DashboardPeriod.Daily.ToLabel();
    public int AccessTokenIssued { get; set; }
    public int RefreshTokenIssued { get; set; }
    public int TokenIssueanceByGrantType { get; set; }
    public int TotalLoginAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public int MfaChallenge { get; set; }
    public int AccountLockout { get; set; }
    public int MultipleFailedLogin { get; set; }
    public int SuspiciousActivity { get; set; }
    public int ExpiringClientCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public DimensionValue TokenVolumeSpike { get; set; } = default!;
    public IEnumerable<TimeSeriesPoint> TokensLast24h { get; set; } = [];
    public IEnumerable<TimeSeriesPoint> AuthLast24h { get; set; } = [];
    public IEnumerable<RankingItem> TopClients { get; set; } = [];
    public IEnumerable<MetricSummary> AuthSummary { get; set; } = [];
    public IEnumerable<DimensionValue> FailedLoginSpikes { get; set; } = [];


    public DashboardResponse()
    {
        TokenVolumeSpike = new DimensionValue();
    }
}

public sealed class TimeSeriesPoint
{
    public DateTime Timestamp { get; init; }
    public long Value { get; init; }
    public long Successful { get; init; }
    public long Failed { get; init; }
    public long AccessTokens { get; init; }
    public long RefreshTokens { get; init; }
    public long MfaChallenges { get; init; }
    public long AccountLockouts { get; init; }
}

public sealed class MetricSummary
{
    public string Metric { get; init; } = default!;
    public long Total { get; init; }
}

public sealed class RankingItem
{
    public int Rank { get; init; }
    public int ClientId { get; init; }
    public string ClientName { get; init; } = default!;
    public string GrantType { get; init; } = default!;
    public long Tokens { get; init; }
}

public sealed class DimensionValue
{
    public string Dimension { get; set; } = default!;
    public long Value { get; set; }
}
