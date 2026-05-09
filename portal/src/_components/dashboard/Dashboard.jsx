import React, {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import AuthenticationActivity from "./AuthenticationActivity";
import DashboardActiveAlerts from "./DashboardActiveAlerts";
import DashboardMetricCards from "./DashboardMetricCards";
import DashboardSystemStatusCard from "./DashboardSystemStatusCard";
import DashboardTechnicalDetails from "./DashboardTechnicalDetails";
import TopClientsVolume from "./TopClientsVolume";
import useApiClient from "../../_hooks/useApiClient";

const formatNumber = (value) => {
  if (value === null || value === undefined) return "0";
  return new Intl.NumberFormat().format(value);
};

const statusToBadge = (status) => {
  const normalized = String(status || "").toLowerCase();
  if (normalized === "healthy") return "success";
  if (normalized === "degraded") return "warning";
  if (normalized === "unhealthy") return "danger";
  return "secondary";
};

const normalizeDashboard = (value) => {
  if (!value) {
    return null;
  }

  const spikes = value.failedLoginSpikes ?? value.FailedLoginSpikes ?? [];
  const latestSpikeTs = spikes
    .map(
      (s) =>
        s?.timestamp ?? s?.Timestamp ?? s?.detectedAt ?? s?.DetectedAt ?? null,
    )
    .filter(Boolean)
    .reduce(
      (max, ts) => (max === null || new Date(ts) > new Date(max) ? ts : max),
      null,
    );

  const globalLastUpdated = value.lastUpdated ?? value.LastUpdated ?? null;

  return {
    period: value.period ?? value.Period ?? "daily",
    periodLabel: value.periodLabel ?? value.PeriodLabel ?? PERIOD_LABELS.daily,
    accessTokenIssued: value.accessTokenIssued ?? value.AccessTokenIssued ?? 0,
    refreshTokenIssued:
      value.refreshTokenIssued ?? value.RefreshTokenIssued ?? 0,
    totalLoginAttempts:
      value.totalLoginAttempts ?? value.TotalLoginAttempts ?? 0,
    successfulLogins: value.successfulLogins ?? value.SuccessfulLogins ?? 0,
    failedLogins: value.failedLogins ?? value.FailedLogins ?? 0,
    mfaChallenge: value.mfaChallenge ?? value.MfaChallenge ?? 0,
    accountLockout: value.accountLockout ?? value.AccountLockout ?? 0,
    multipleFailedLogin:
      value.multipleFailedLogin ?? value.MultipleFailedLogin ?? 0,
    suspiciousActivity:
      value.suspiciousActivity ?? value.SuspiciousActivity ?? 0,
    tokensLast24h: value.tokensLast24h ?? value.TokensLast24h ?? [],
    authLast24h: value.authLast24h ?? value.AuthLast24h ?? [],
    topClients: value.topClients ?? value.TopClients ?? [],
    failedLoginSpikes: spikes,
    expiringClientCount:
      value.expiringClientCount ?? value.ExpiringClientCount ?? 0,
    tokenVolumeSpike: value.tokenVolumeSpike ?? value.TokenVolumeSpike ?? null,
    activeSessions: value.activeSessions ?? value.ActiveSessions ?? 0,
    registeredClients: value.registeredClients ?? value.RegisteredClients ?? 0,
    averageTokenTtlSeconds:
      value.averageTokenTtlSeconds ?? value.AverageTokenTtlSeconds ?? 0,
    uptimeSeconds: value.uptimeSeconds ?? value.UptimeSeconds ?? 0,
    latencyP95Ms: value.latencyP95Ms ?? value.LatencyP95Ms ?? 0,
    latencyP99Ms: value.latencyP99Ms ?? value.LatencyP99Ms ?? 0,
    region: value.region ?? value.Region ?? "",
    version: value.version ?? value.Version ?? "",
    lastKeyRotationUtc:
      value.lastKeyRotationUtc ?? value.LastKeyRotationUtc ?? null,
    lastUpdated: globalLastUpdated,
    multipleFailedLoginAt:
      latestSpikeTs ??
      value.multipleFailedLoginAt ??
      value.MultipleFailedLoginAt ??
      globalLastUpdated,
    suspiciousActivityAt:
      value.suspiciousActivityAt ??
      value.SuspiciousActivityAt ??
      globalLastUpdated,
    expiringClientAt: value.expiringClientAt ?? value.ExpiringClientAt ?? null,
  };
};

const calcTimeDiff = (utcValue) => {
  if (!utcValue) return null;
  const date = new Date(utcValue);
  if (Number.isNaN(date.getTime())) return null;
  const diffSec = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000));
  if (diffSec < 60) return { value: diffSec, unit: "s" };
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return { value: diffMin, unit: "m" };
  const diffHours = Math.floor(diffMin / 60);
  if (diffHours < 24) return { value: diffHours, unit: "h" };
  return { value: Math.floor(diffHours / 24), unit: "d" };
};

const toRelativeLabel = (utcValue) => {
  const d = calcTimeDiff(utcValue);
  if (!d) return "";
  const long = { s: "seconds", m: "minutes", h: "hours", d: "days" };
  return `Last updated: ${d.value} ${long[d.unit]} ago`;
};

const toRelativeShort = (utcValue) => {
  const d = calcTimeDiff(utcValue);
  return d ? `${d.value}${d.unit} ago` : null;
};

const formatUptime = (seconds) => {
  const total = Number(seconds) || 0;
  if (total <= 0) return "0m";

  const days = Math.floor(total / 86400);
  const hours = Math.floor((total % 86400) / 3600);
  const minutes = Math.floor((total % 3600) / 60);

  if (days > 0) return `${days}d ${hours}h`;
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${Math.max(1, minutes)}m`;
};

const formatTokenTtl = (seconds) => {
  const ttl = Number(seconds) || 0;
  return ttl > 0 ? `${formatNumber(ttl)}s` : "N/A";
};

const formatLatency = (p95Ms, p99Ms) => {
  const p95 = Number(p95Ms) || 0;
  const p99 = Number(p99Ms) || 0;

  if (p95 <= 0 && p99 <= 0) {
    return "No traffic yet";
  }

  if (p99 > 0) {
    return `${Math.round(p95)}ms / ${Math.round(p99)}ms`;
  }

  return `${Math.round(p95)}ms`;
};

const sumAuthSeries = (arr) =>
  arr.reduce(
    (acc, p) => ({
      success:
        acc.success +
        (Number(
          p?.successful ??
            p?.Successful ??
            p?.successfulLogins ??
            p?.SuccessfulLogins ??
            p?.success ??
            p?.Success ??
            0,
        ) || 0),
      failed:
        acc.failed +
        (Number(
          p?.failed ??
            p?.Failed ??
            p?.failedLogins ??
            p?.FailedLogins ??
            p?.failedCount ??
            p?.FailedCount ??
            0,
        ) || 0),
    }),
    { success: 0, failed: 0 },
  );

const sumTokenSeries = (arr) =>
  arr.reduce(
    (acc, p) => ({
      access:
        acc.access +
        (Number(p?.accessTokens ?? p?.AccessTokens ?? p?.access ?? p?.Access ?? 0) ||
          0),
      refresh:
        acc.refresh +
        (Number(
          p?.refreshTokens ?? p?.RefreshTokens ?? p?.refresh ?? p?.Refresh ?? 0,
        ) || 0),
    }),
    { access: 0, refresh: 0 },
  );

const sumSecuritySeries = (arr) =>
  arr.reduce(
    (acc, p) => ({
      mfa:
        acc.mfa +
        (Number(
          p?.mfaChallenges ??
            p?.MfaChallenges ??
            p?.mfa ??
            p?.Mfa ??
            p?.mfaChallenge ??
            p?.MfaChallenge ??
            0,
        ) || 0),
      lockouts:
        acc.lockouts +
        (Number(
          p?.accountLockouts ??
            p?.AccountLockouts ??
            p?.accountLockout ??
            p?.AccountLockout ??
            p?.lockouts ??
            p?.Lockouts ??
            0,
        ) || 0),
    }),
    { mfa: 0, lockouts: 0 },
  );

const pctChange = (prev, curr) => {
  if (!prev) return null;
  const pct = Math.round(((curr - prev) / prev) * 1000) / 10;
  return { trend: `${pct >= 0 ? "+" : ""}${pct}%`, trendUp: pct >= 0 };
};

const DASHBOARD_PERIOD_OPTIONS = [
  { value: "daily", label: "Daily" },
  { value: "weekly", label: "Weekly" },
  { value: "monthly", label: "Monthly" },
];

const PERIOD_LABELS = {
  daily: "Last 24 Hours",
  weekly: "Last 7 Days",
  monthly: "Last 30 Days",
};

const POLL_INTERVAL_MS = 60_000; // refresh every 60 seconds

function Dashboard() {
  const { get } = useApiClient();
  const [dashboard, setDashboard] = useState(null);
  const [health, setHealth] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedPeriod, setSelectedPeriod] = useState("daily");

  const loadDashboard = useCallback(
    async (silent = false) => {
      if (!silent) setLoading(true);
      setError("");

      try {
        const [dashboardResponse, healthResponse] = await Promise.all([
          get(`admin/dashboard?period=${selectedPeriod}`),
          get("health"),
        ]);
        const dashboardResult =
          dashboardResponse?.data?.value ?? dashboardResponse?.data ?? null;
        const healthResult = healthResponse?.data ?? null;
        setDashboard(dashboardResult);
        setHealth(healthResult);
      } catch (err) {
        setError(err?.message || "Unable to load dashboard.");
        if (!silent) {
          setDashboard(null);
          setHealth(null);
        }
      } finally {
        if (!silent) setLoading(false);
      }
    },
    [get, selectedPeriod],
  );

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  useEffect(() => {
    const intervalId = setInterval(() => loadDashboard(true), POLL_INTERVAL_MS);
    return () => clearInterval(intervalId);
  }, [loadDashboard]);

  const normalizedDashboard = useMemo(
    () => normalizeDashboard(dashboard),
    [dashboard],
  );

  const viewModel = useMemo(() => {
    const healthEntries = health?.entries || {};
    const emptyDashboard = normalizedDashboard || {
      period: selectedPeriod,
      periodLabel: PERIOD_LABELS[selectedPeriod] || PERIOD_LABELS.daily,
      accessTokenIssued: 0,
      refreshTokenIssued: 0,
      totalLoginAttempts: 0,
      successfulLogins: 0,
      failedLogins: 0,
      mfaChallenge: 0,
      accountLockout: 0,
      multipleFailedLogin: 0,
      suspiciousActivity: 0,
      tokensLast24h: [],
      authLast24h: [],
      topClients: [],
      failedLoginSpikes: [],
      expiringClientCount: 0,
      tokenVolumeSpike: null,
      activeSessions: 0,
      registeredClients: 0,
      averageTokenTtlSeconds: 0,
      uptimeSeconds: 0,
      latencyP95Ms: 0,
      latencyP99Ms: 0,
      region: "",
      version: "",
      lastKeyRotationUtc: null,
      lastUpdated: null,
      multipleFailedLoginAt: null,
      suspiciousActivityAt: null,
      expiringClientAt: null,
    };

    // ── Computed metrics from time-series ──────────────────────
    const authSeriesData = emptyDashboard.authLast24h;
    const tokenSeriesData = emptyDashboard.tokensLast24h;
    const authMid = Math.floor(authSeriesData.length / 2);
    const tokenMid = Math.floor(tokenSeriesData.length / 2);

    const firstHalfAuth = sumAuthSeries(authSeriesData.slice(0, authMid));
    const secondHalfAuth = sumAuthSeries(authSeriesData.slice(authMid));
    const firstHalfTokens = sumTokenSeries(tokenSeriesData.slice(0, tokenMid));
    const secondHalfTokens = sumTokenSeries(tokenSeriesData.slice(tokenMid));
    const firstHalfSecurity = sumSecuritySeries(authSeriesData.slice(0, authMid));
    const secondHalfSecurity = sumSecuritySeries(authSeriesData.slice(authMid));

    const authTrends = {
      total: pctChange(
        firstHalfAuth.success + firstHalfAuth.failed,
        secondHalfAuth.success + secondHalfAuth.failed,
      ),
      successful: pctChange(firstHalfAuth.success, secondHalfAuth.success),
      failed: (() => {
        const t = pctChange(firstHalfAuth.failed, secondHalfAuth.failed);
        return t ? { trend: t.trend, trendUp: !t.trendUp } : null;
      })(),
    };

    const tokenTrends = {
      access: pctChange(firstHalfTokens.access, secondHalfTokens.access),
      refresh: pctChange(firstHalfTokens.refresh, secondHalfTokens.refresh),
    };

    const securityTrends = {
      mfa: pctChange(firstHalfSecurity.mfa, secondHalfSecurity.mfa),
      lockouts: (() => {
        const t = pctChange(
          firstHalfSecurity.lockouts,
          secondHalfSecurity.lockouts,
        );
        return t ? { trend: t.trend, trendUp: !t.trendUp } : null;
      })(),
    };

    const totalAttempts = emptyDashboard.totalLoginAttempts;
    const errorRate =
      totalAttempts > 0 ? emptyDashboard.failedLogins / totalAttempts : 0;
    const errorRateStr = `${(errorRate * 100).toFixed(2)}%`;
    const errorRateBadge =
      errorRate > 0.05 ? "danger" : errorRate > 0.01 ? "warning" : "secondary";

    const errFirst =
      firstHalfAuth.success + firstHalfAuth.failed > 0
        ? firstHalfAuth.failed / (firstHalfAuth.success + firstHalfAuth.failed)
        : 0;
    const errSecond =
      secondHalfAuth.success + secondHalfAuth.failed > 0
        ? secondHalfAuth.failed / (secondHalfAuth.success + secondHalfAuth.failed)
        : 0;
    const errorRateTrend = (() => {
      const t = pctChange(errFirst, errSecond);
      return t ? { trend: t.trend, trendUp: !t.trendUp } : null;
    })();

    const successRate =
      totalAttempts > 0 ? emptyDashboard.successfulLogins / totalAttempts : 0;
    const successRateStr = `${(successRate * 100).toFixed(1)}%`;
    const srFirst =
      firstHalfAuth.success + firstHalfAuth.failed > 0
        ? firstHalfAuth.success / (firstHalfAuth.success + firstHalfAuth.failed)
        : 0;
    const srSecond =
      secondHalfAuth.success + secondHalfAuth.failed > 0
        ? secondHalfAuth.success / (secondHalfAuth.success + secondHalfAuth.failed)
        : 0;
    const successRateTrend = pctChange(srFirst, srSecond);
    // ──────────────────────────────────────────────────────────

    const kpis = [
      {
        title: "IDP Service Status",
        value: health?.status || (loading ? "Loading" : "Unknown"),
        badge: statusToBadge(health?.status || "healthy"),
        caption: "Overall system health",
      },
      {
        title: "Authorization Endpoint",
        value:
          healthEntries?.authorization?.status ||
          (loading ? "Loading" : "Unknown"),
        badge: statusToBadge(healthEntries?.authorization?.status),
        caption:
          healthEntries?.authorization?.description ||
          "Authorization reachability",
      },
      {
        title: "Token Endpoint Status",
        value:
          healthEntries?.token?.status || (loading ? "Loading" : "Unknown"),
        badge: statusToBadge(healthEntries?.token?.status),
        caption: healthEntries?.token?.description || "Token reachability",
      },
      {
        title: "Error Rate (15 min)",
        value: errorRateStr,
        badge: errorRateBadge,
        caption: "Login + token errors",
      },
    ];

    const topClients = (emptyDashboard.topClients || []).map((client) => ({
      clientId: client.clientId,
      name: client.clientName,
      grant: client.grantType,
      tokens: formatNumber(client.tokens),
    }));

    const alerts = [
      {
        title: "Multiple failed login attempts",
        detail: `${formatNumber(
          emptyDashboard.multipleFailedLogin,
        )} failures in the last 15 minutes.`,
        level: emptyDashboard.multipleFailedLogin > 0 ? "danger" : "secondary",
      },
      {
        title: "Expiring client secrets",
        detail: `${formatNumber(
          emptyDashboard.expiringClientCount,
        )} confidential clients expire within 7 days.`,
        level: emptyDashboard.expiringClientCount > 0 ? "warning" : "secondary",
      },
    ];

    const alertCards = [
      {
        title: "Multiple failed login attempts detected",
        detail:
          emptyDashboard.multipleFailedLogin > 0
            ? `${formatNumber(emptyDashboard.multipleFailedLogin)} events in the last 15 minutes.`
            : "No failed login spikes detected.",
        badgeText: `${formatNumber(emptyDashboard.multipleFailedLogin)} events`,
        meta:
          emptyDashboard.multipleFailedLogin > 0
            ? toRelativeShort(emptyDashboard.multipleFailedLoginAt) ||
              "Just now"
            : "Last 15 min",
        status: emptyDashboard.multipleFailedLogin > 0 ? "danger" : "secondary",
        icon: "shield",
      },
      {
        title: "Client secrets expiring soon",
        detail:
          emptyDashboard.expiringClientCount > 0
            ? `${formatNumber(emptyDashboard.expiringClientCount)} secrets expiring within 7 days.`
            : "No expiring client secrets.",
        badgeText: `${formatNumber(emptyDashboard.expiringClientCount)} events`,
        meta:
          emptyDashboard.expiringClientCount > 0
            ? toRelativeShort(emptyDashboard.expiringClientAt) || "Today"
            : "No action needed",
        status:
          emptyDashboard.expiringClientCount > 0 ? "warning" : "secondary",
        icon: "clock",
      },
      {
        title: "Suspicious activity from unusual locations",
        detail:
          emptyDashboard.suspiciousActivity > 0
            ? `${formatNumber(emptyDashboard.suspiciousActivity)} suspicious events detected.`
            : "No unusual activity detected.",
        badgeText: `${formatNumber(emptyDashboard.suspiciousActivity)} events`,
        meta:
          emptyDashboard.suspiciousActivity > 0
            ? toRelativeShort(emptyDashboard.suspiciousActivityAt) || "Just now"
            : "Last 15 min",
        status: emptyDashboard.suspiciousActivity > 0 ? "warning" : "secondary",
        icon: "alert",
      },
    ];

    const spike = emptyDashboard.tokenVolumeSpike;
    const spikeDimension =
      spike?.dimension ?? spike?.Dimension ?? "Unknown client";
    const spikeValue = spike?.value ?? spike?.Value ?? 0;
    alerts.push({
      title: "Suspicious activity indicators",
      detail:
        spikeValue > 0
          ? `${spikeDimension} has an unusual token volume spike: ${formatNumber(
              spikeValue,
            )} tokens in the current hour.`
          : `${formatNumber(
              emptyDashboard.suspiciousActivity,
            )} suspicious events detected.`,
      level:
        spikeValue > 0 || emptyDashboard.suspiciousActivity > 0
          ? "danger"
          : "secondary",
    });

    if (error) {
      alerts.unshift({
        title: "Dashboard data unavailable",
        detail: "Unable to load dashboard metrics. Please retry.",
        level: "warning",
      });
    }

    return {
      selectedPeriod: emptyDashboard.period || selectedPeriod,
      periodLabel:
        emptyDashboard.periodLabel ||
        PERIOD_LABELS[emptyDashboard.period] ||
        PERIOD_LABELS[selectedPeriod] ||
        PERIOD_LABELS.daily,
      kpis,
      lastUpdatedLabel: toRelativeLabel(emptyDashboard.lastUpdated),
      authSeries: emptyDashboard.authLast24h,
      alerts,
      alertCards,
      topClients,
      systemStatus: {
        title:
          String(health?.status || "").toLowerCase() === "healthy"
            ? "System Status: All Systems Operational"
            : "System Status",
        caption:
          String(health?.status || "").toLowerCase() === "healthy"
            ? "All services running normally"
            : healthEntries?.authorization?.description ||
              "Service availability",
        liveLabel: "Live",
        liveStatus: statusToBadge(health?.status),
        items: [
          {
            label: "IDP Service",
            value: health?.status || "Unknown",
            status: statusToBadge(health?.status),
          },
          {
            label: "Auth Endpoint",
            value: healthEntries?.authorization?.status || "Unknown",
            status: statusToBadge(healthEntries?.authorization?.status),
          },
          {
            label: "Token Endpoint",
            value: healthEntries?.token?.status || "Unknown",
            status: statusToBadge(healthEntries?.token?.status),
          },
        ],
      },
      metricCards: [
        {
          label: "Access tokens issued (24h)",
          value: formatNumber(emptyDashboard.accessTokenIssued),
        },
        {
          label: "Refresh tokens issued (24h)",
          value: formatNumber(emptyDashboard.refreshTokenIssued),
        },
        {
          label: "Total login attempts",
          value: formatNumber(emptyDashboard.totalLoginAttempts),
        },
        {
          label: "Successful logins",
          value: formatNumber(emptyDashboard.successfulLogins),
        },
        {
          label: "Failed logins",
          value: formatNumber(emptyDashboard.failedLogins),
        },
        {
          label: "MFA challenges",
          value: formatNumber(emptyDashboard.mfaChallenge),
        },
        {
          label: "Account lockouts",
          value: formatNumber(emptyDashboard.accountLockout),
        },
      ],
      totals: {
        successfulLogins: emptyDashboard.successfulLogins,
        failedLogins: emptyDashboard.failedLogins,
      },
      activeAlertCount: alertCards.filter((c) => c.status !== "secondary")
        .length,
      tokenMetrics: [
        {
          label: "Access Tokens",
          value: formatNumber(emptyDashboard.accessTokenIssued),
          ...(tokenTrends.access ?? { trend: null, trendUp: true }),
        },
        {
          label: "Refresh Tokens",
          value: formatNumber(emptyDashboard.refreshTokenIssued),
          ...(tokenTrends.refresh ?? { trend: null, trendUp: true }),
        },
        {
          label: "Success Rate",
          value: successRateStr,
          ...(successRateTrend ?? { trend: null, trendUp: true }),
        },
      ],
      loginActivity: [
        {
          label: "Total Attempts",
          value: formatNumber(emptyDashboard.totalLoginAttempts),
          ...(authTrends.total ?? { trend: null, trendUp: true }),
        },
        {
          label: "Successful",
          value: formatNumber(emptyDashboard.successfulLogins),
          ...(authTrends.successful ?? { trend: null, trendUp: true }),
        },
        {
          label: "Failed",
          value: formatNumber(emptyDashboard.failedLogins),
          ...(authTrends.failed ?? { trend: null, trendUp: false }),
        },
      ],
      securityMetrics: [
        {
          label: "MFA Challenges",
          value: formatNumber(emptyDashboard.mfaChallenge),
          ...(securityTrends.mfa ?? { trend: null, trendUp: true }),
        },
        {
          label: "Account Lockouts",
          value: formatNumber(emptyDashboard.accountLockout),
          ...(securityTrends.lockouts ?? { trend: null, trendUp: false }),
        },
        {
          label: "Error Rate",
          value: errorRateStr,
          ...(errorRateTrend ?? { trend: null, trendUp: false }),
        },
      ],
      technicalDetails: (() => {
        const healthValues = Object.values(healthEntries);
        return {
          allOperational:
            healthValues.length > 0
              ? healthValues.every(
                  (e) => String(e?.status || "").toLowerCase() === "healthy",
                )
              : String(health?.status || "").toLowerCase() === "healthy",
          metaItems: [
            {
              label: "Uptime",
              value: formatUptime(emptyDashboard.uptimeSeconds),
            },
            {
              label: "Region",
              value: emptyDashboard.region || "Not configured",
            },
            {
              label: "Version",
              value: emptyDashboard.version || "Not configured",
            },
            {
              label: "Latency",
              value: formatLatency(
                emptyDashboard.latencyP95Ms,
                emptyDashboard.latencyP99Ms,
              ),
            },
          ],
          endpoints: Object.entries(healthEntries).map(([key, val]) => ({
            name: key.charAt(0).toUpperCase() + key.slice(1) + " Endpoint",
            status: val?.status || "Unknown",
            badge: statusToBadge(val?.status),
          })),
          infoItems: [
            {
              label: "Active Sessions",
              value: formatNumber(emptyDashboard.activeSessions),
            },
            {
              label: "Registered Clients",
              value: formatNumber(emptyDashboard.registeredClients),
            },
            {
              label: "Avg Access Token Lifetime",
              value: formatTokenTtl(emptyDashboard.averageTokenTtlSeconds),
            },
          ],
        };
      })(),
    };
  }, [normalizedDashboard, health, loading, error, selectedPeriod]);

  return (
    <>
      <DashboardSystemStatusCard systemStatus={viewModel.systemStatus} />
      <DashboardActiveAlerts
        activeAlertCount={viewModel.activeAlertCount}
        alertCards={viewModel.alertCards}
        periodOptions={DASHBOARD_PERIOD_OPTIONS}
        selectedPeriod={viewModel.selectedPeriod}
        onPeriodChange={setSelectedPeriod}
      />
      <DashboardMetricCards
        tokenMetrics={viewModel.tokenMetrics}
        loginActivity={viewModel.loginActivity}
        securityMetrics={viewModel.securityMetrics}
        skeleton={loading && !normalizedDashboard}
      />

      <div className="dashboard-auth-layout mb-3">
        <div className="dashboard-auth-main">
          <AuthenticationActivity
            series={viewModel.authSeries}
            totals={viewModel.totals}
            period={viewModel.selectedPeriod}
            periodLabel={viewModel.periodLabel}
          />
        </div>

        <div className="dashboard-summary-panel">
          <DashboardTechnicalDetails
            technicalDetails={viewModel.technicalDetails}
          />
        </div>
      </div>
      <div className="mb-3">
        <TopClientsVolume
          topClients={viewModel.topClients}
          periodLabel={viewModel.periodLabel}
        />
      </div>
    </>
  );
}

export default Dashboard;
