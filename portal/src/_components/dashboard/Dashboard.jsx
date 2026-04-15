import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import DashboardHeader from "./DashboardHeader";
import KpiRow from "./KpiRow";
import AuthenticationActivity from "./AuthenticationActivity";
import TopClientsVolume from "./TopClientsVolume";
import SecurityAlerts from "./SecurityAlerts";
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
    authLast24h: value.authLast24h ?? value.AuthLast24h ?? [],
    topClients: value.topClients ?? value.TopClients ?? [],
    failedLoginSpikes: spikes,
    expiringClientCount:
      value.expiringClientCount ?? value.ExpiringClientCount ?? 0,
    tokenVolumeSpike: value.tokenVolumeSpike ?? value.TokenVolumeSpike ?? null,
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

const sumSeries = (arr) =>
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

const pctChange = (prev, curr) => {
  if (!prev) return null;
  const pct = Math.round(((curr - prev) / prev) * 1000) / 10;
  return { trend: `${pct >= 0 ? "+" : ""}${pct}%`, trendUp: pct >= 0 };
};

const TrendIndicator = ({ trendUp, trend }) => {
  if (!trend) return null;
  return (
    <span
      className={`dashboard-kpi-trend ${trendUp ? "trend-up" : "trend-down"}`}
    >
      {trendUp ? (
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
          <path
            d="M12 19V5M5 12l7-7 7 7"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      ) : (
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
          <path
            d="M12 5v14M19 12l-7 7-7-7"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      )}
      {trend}
    </span>
  );
};

const MetricCard = ({ title, icon, metrics, skeleton }) => (
  <div className="card-lite mb-0">
    <div className="card-header dashboard-kpi-card-header">
      <div className="dashboard-kpi-card-icon">{icon}</div>
      <h6 className="mb-0">{title}</h6>
    </div>
    <div className="card-body dashboard-kpi-card-body">
      {metrics.map((metric, i) => (
        <React.Fragment key={metric.label}>
          {i > 0 && <div className="dashboard-kpi-divider" />}
          <div className="dashboard-kpi-metric">
            <div className="dashboard-kpi-metric-top">
              <span className="dashboard-kpi-metric-label">{metric.label}</span>
              <TrendIndicator trendUp={metric.trendUp} trend={metric.trend} />
            </div>
            <div className="dashboard-kpi-metric-value">
              {skeleton ? "--" : metric.value}
            </div>
          </div>
        </React.Fragment>
      ))}
    </div>
  </div>
);

const getAlertCardIcon = (type) => {
  switch (type) {
    case "shield":
      return (
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M12 2 4 5v6c0 5.55 3.84 10.74 8 11 4.16-.26 8-5.45 8-11V5l-8-3Z"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path
            d="m9 12 2 2 4-4"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      );
    case "clock":
      return (
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2" />
          <path
            d="M12 7v5l3 3"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      );
    case "alert":
      return (
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M12 9v4"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
          />
          <path
            d="M12 17h.01"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
          />
          <path
            d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      );
    default:
      return null;
  }
};

const POLL_INTERVAL_MS = 60_000; // refresh every 60 seconds

function Dashboard() {
  const { get } = useApiClient();
  const [dashboard, setDashboard] = useState(null);
  const [health, setHealth] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const didLoadRef = useRef(false);

  const loadDashboard = useCallback(
    async (silent = false) => {
      if (!silent) setLoading(true);
      setError("");

      try {
        const [dashboardResponse, healthResponse] = await Promise.all([
          get("admin/dashboard"),
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
    [get],
  );

  useEffect(() => {
    if (didLoadRef.current) {
      return;
    }
    didLoadRef.current = true;
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
      accessTokenIssued: 0,
      refreshTokenIssued: 0,
      totalLoginAttempts: 0,
      successfulLogins: 0,
      failedLogins: 0,
      mfaChallenge: 0,
      accountLockout: 0,
      multipleFailedLogin: 0,
      suspiciousActivity: 0,
      authLast24h: [],
      topClients: [],
      failedLoginSpikes: [],
      expiringClientCount: 0,
      tokenVolumeSpike: null,
      lastUpdated: null,
      multipleFailedLoginAt: null,
      suspiciousActivityAt: null,
      expiringClientAt: null,
    };

    // ── Computed metrics from time-series ──────────────────────
    const seriesData = emptyDashboard.authLast24h;
    const mid = Math.floor(seriesData.length / 2);
    const firstHalf = sumSeries(seriesData.slice(0, mid));
    const secondHalf = sumSeries(seriesData.slice(mid));

    const seriesTrends = {
      total: pctChange(
        firstHalf.success + firstHalf.failed,
        secondHalf.success + secondHalf.failed,
      ),
      successful: pctChange(firstHalf.success, secondHalf.success),
      // Failed: invert trendUp so rising failures show as red
      failed: (() => {
        const t = pctChange(firstHalf.failed, secondHalf.failed);
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
      firstHalf.success + firstHalf.failed > 0
        ? firstHalf.failed / (firstHalf.success + firstHalf.failed)
        : 0;
    const errSecond =
      secondHalf.success + secondHalf.failed > 0
        ? secondHalf.failed / (secondHalf.success + secondHalf.failed)
        : 0;
    const errorRateTrend = (() => {
      const t = pctChange(errFirst, errSecond);
      return t ? { trend: t.trend, trendUp: !t.trendUp } : null;
    })();

    const successRate =
      totalAttempts > 0 ? emptyDashboard.successfulLogins / totalAttempts : 0;
    const successRateStr = `${(successRate * 100).toFixed(1)}%`;
    const srFirst =
      firstHalf.success + firstHalf.failed > 0
        ? firstHalf.success / (firstHalf.success + firstHalf.failed)
        : 0;
    const srSecond =
      secondHalf.success + secondHalf.failed > 0
        ? secondHalf.success / (secondHalf.success + secondHalf.failed)
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
          ...(seriesTrends.successful ?? { trend: null, trendUp: true }),
        },
        {
          label: "Refresh Tokens",
          value: formatNumber(emptyDashboard.refreshTokenIssued),
          ...(seriesTrends.total ?? { trend: null, trendUp: true }),
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
          ...(seriesTrends.total ?? { trend: null, trendUp: true }),
        },
        {
          label: "Successful",
          value: formatNumber(emptyDashboard.successfulLogins),
          ...(seriesTrends.successful ?? { trend: null, trendUp: true }),
        },
        {
          label: "Failed",
          value: formatNumber(emptyDashboard.failedLogins),
          ...(seriesTrends.failed ?? { trend: null, trendUp: false }),
        },
      ],
      securityMetrics: [
        {
          label: "MFA Challenges",
          value: formatNumber(emptyDashboard.mfaChallenge),
          trend: null,
          trendUp: true,
        },
        {
          label: "Account Lockouts",
          value: formatNumber(emptyDashboard.accountLockout),
          trend: null,
          trendUp: false,
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
            { label: "Uptime", value: "99.99%" },
            { label: "Region", value: "US-East-1" },
            { label: "Version", value: "v3.4.2" },
            { label: "Latency", value: "42ms" },
          ],
          endpoints: Object.entries(healthEntries).map(([key, val]) => ({
            name: key.charAt(0).toUpperCase() + key.slice(1) + " Endpoint",
            status: val?.status || "Unknown",
            badge: statusToBadge(val?.status),
          })),
          infoItems: [
            { label: "Active Sessions", value: "2,847" },
            { label: "Registered Clients", value: "156" },
            { label: "Token TTL (avg)", value: "3600s" },
            { label: "Last Key Rotation", value: "2 days ago" },
          ],
        };
      })(),
    };
  }, [normalizedDashboard, health, loading, error]);

  return (
    <>
      {/* <DashboardHeader lastUpdatedLabel={viewModel.lastUpdatedLabel}   /> */}

      <div className="dashboard-system-status-card card-lite mb-3">
        <div className="dashboard-system-status-card-body">
          <div className="dashboard-system-status-card-main">
            <div className="dashboard-system-status-card-icon">
              <svg
                width="24"
                height="24"
                viewBox="0 0 24 24"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
              >
                <circle
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="2"
                />
                <path
                  d="M8 12.5L10.5 15L16 9"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </div>
            <div className="dashboard-system-status-card-info">
              <div className="dashboard-system-status-card-title-row">
                <h5 className="dashboard-system-status-card-title">
                  {viewModel.systemStatus.title}
                </h5>
                <span
                  className={`status-pill status-pill-${viewModel.systemStatus.liveStatus} dashboard-system-status-live`}
                >
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M4 12h4l2 5 3-10 2 5h5"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                  {viewModel.systemStatus.liveLabel}
                </span>
              </div>
              <div className="dashboard-system-status-card-caption">
                {viewModel.systemStatus.caption}
              </div>
            </div>
          </div>
          <div className="dashboard-system-status-items">
            {viewModel.systemStatus.items.map((item) => (
              <div key={item.label} className="dashboard-system-status-item">
                <span
                  className={`dashboard-system-status-dot ${item.status}`}
                />
                <div>
                  <div className="dashboard-system-status-item-label">
                    {item.label}
                  </div>
                  <div className="dashboard-system-status-item-value">
                    {item.value}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="dashboard-active-alerts-section mb-3">
        <div className="dashboard-active-alerts-heading">
          <span>Active Alerts</span>
          {viewModel.activeAlertCount > 0 && (
            <span className="dashboard-active-alerts-badge">
              {viewModel.activeAlertCount}
            </span>
          )}
        </div>
        <div className="dashboard-alert-card-grid">
          {viewModel.alertCards.map((card) => (
            <div
              key={card.title}
              className={`dashboard-alert-card card-lite dashboard-alert-card-${card.status}`}
            >
              <div className="dashboard-alert-card-body">
                <div className="dashboard-alert-card-icon-wrap">
                  <div
                    className={`dashboard-alert-card-icon dashboard-alert-card-icon-${card.status}`}
                  >
                    {getAlertCardIcon(card.icon)}
                  </div>
                  <div className="dashboard-alert-card-copy">
                    <div className="dashboard-alert-card-title">
                      {card.title}
                    </div>
                    <div className="dashboard-alert-card-footer">
                      <span
                        className={`dashboard-alert-card-pill dashboard-alert-card-pill-${card.status}`}
                      >
                        {card.badgeText}
                      </span>
                      <span className="dashboard-alert-card-meta">
                        {card.meta}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="dashboard-alert-card-detail">{card.detail}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="dashboard-metric-cards-grid mb-3">
        <MetricCard
          title="Token Metrics"
          icon={
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
              <circle
                cx="12"
                cy="12"
                r="3"
                stroke="currentColor"
                strokeWidth="2"
              />
              <path
                d="M3 12h6M15 12h6M12 3v6M12 15v6"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          }
          metrics={viewModel.tokenMetrics}
          skeleton={loading && !normalizedDashboard}
        />
        <MetricCard
          title="Login Activity"
          icon={
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
              <path
                d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          }
          metrics={viewModel.loginActivity}
          skeleton={loading && !normalizedDashboard}
        />
        <MetricCard
          title="Security Metrics"
          icon={
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
              <path
                d="M12 2 4 5v6c0 5.55 3.84 10.74 8 11 4.16-.26 8-5.45 8-11V5l-8-3Z"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          }
          metrics={viewModel.securityMetrics}
          skeleton={loading && !normalizedDashboard}
        />
      </div>

      <div className="dashboard-auth-layout mb-3">
        <div className="dashboard-auth-main">
          <AuthenticationActivity
            series={viewModel.authSeries}
            totals={viewModel.totals}
          />
        </div>

        <div className="dashboard-summary-panel">
          <div className="card-lite h-100">
            <div className="card-header dashboard-tech-header">
              <div className="dashboard-tech-header-icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                  <rect
                    x="2"
                    y="3"
                    width="20"
                    height="14"
                    rx="2"
                    stroke="currentColor"
                    strokeWidth="2"
                  />
                  <path
                    d="M8 21h8M12 17v4"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                  />
                </svg>
              </div>
              <div>
                <h6 className="mb-0">Technical Details</h6>
                <div className="text-muted" style={{ fontSize: "12px" }}>
                  Endpoint configuration &amp; validation
                </div>
              </div>
            </div>
            <div className="card-body">
              <div className="dashboard-tech-status-section">
                <div className="dashboard-tech-status-row">
                  <span
                    className={`dashboard-system-status-dot ${viewModel.technicalDetails.allOperational ? "success" : "warning"}`}
                  />
                  <div>
                    <div className="fw-semibold" style={{ fontSize: "13px" }}>
                      IDP Service Status
                    </div>
                    <div
                      className={
                        viewModel.technicalDetails.allOperational
                          ? "text-success"
                          : "text-warning"
                      }
                      style={{ fontSize: "12px" }}
                    >
                      {viewModel.technicalDetails.allOperational
                        ? "All endpoints operational"
                        : "Some endpoints degraded"}
                    </div>
                  </div>
                </div>
                <div className="dashboard-tech-meta-grid">
                  {viewModel.technicalDetails.metaItems.map((item) => (
                    <div key={item.label} className="dashboard-tech-meta-item">
                      <span className="dashboard-tech-meta-label">
                        {item.label}
                      </span>
                      <span className="dashboard-tech-meta-value">
                        {item.value}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="dashboard-tech-info-grid">
                {viewModel.technicalDetails.infoItems.map((item) => (
                  <div key={item.label} className="dashboard-tech-info-row">
                    <span className="dashboard-tech-info-label">
                      {item.label}
                    </span>
                    <span className="dashboard-tech-info-value">
                      {item.value}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
      <div className="mb-3">
        <TopClientsVolume topClients={viewModel.topClients} />
      </div>
    </>
  );
}

export default Dashboard;
