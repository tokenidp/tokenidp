import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
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

  return {
    accessTokenIssued: value.accessTokenIssued ?? value.AccessTokenIssued ?? 0,
    refreshTokenIssued: value.refreshTokenIssued ?? value.RefreshTokenIssued ?? 0,
    totalLoginAttempts: value.totalLoginAttempts ?? value.TotalLoginAttempts ?? 0,
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
    failedLoginSpikes: value.failedLoginSpikes ?? value.FailedLoginSpikes ?? [],
    expiringClientCount:
      value.expiringClientCount ?? value.ExpiringClientCount ?? 0,
    tokenVolumeSpike: value.tokenVolumeSpike ?? value.TokenVolumeSpike ?? null,
    lastUpdated: value.lastUpdated ?? value.LastUpdated ?? null,
  };
};

const toRelativeLabel = (utcValue) => {
  if (!utcValue) return "";
  const date = new Date(utcValue);
  if (Number.isNaN(date.getTime())) return "";
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.max(0, Math.floor(diffMs / 1000));
  if (diffSec < 60) return `Last updated: ${diffSec} seconds ago`;
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `Last updated: ${diffMin} minutes ago`;
  const diffHours = Math.floor(diffMin / 60);
  if (diffHours < 24) return `Last updated: ${diffHours} hours ago`;
  const diffDays = Math.floor(diffHours / 24);
  return `Last updated: ${diffDays} days ago`;
};

function Dashboard() {
  const { get } = useApiClient();
  const [dashboard, setDashboard] = useState(null);
  const [health, setHealth] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const didLoadRef = useRef(false);

  const loadDashboard = useCallback(async () => {
    setLoading(true);
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
      setDashboard(null);
      setHealth(null);
    } finally {
      setLoading(false);
    }
  }, [get]);

  useEffect(() => {
    if (didLoadRef.current) {
      return;
    }

    didLoadRef.current = true;
    loadDashboard();
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
    };

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
          healthEntries?.authorization?.status || (loading ? "Loading" : "Unknown"),
        badge: statusToBadge(healthEntries?.authorization?.status),
        caption:
          healthEntries?.authorization?.description ||
          "Authorization reachability",
      },
      {
        title: "Token Endpoint Status",
        value: healthEntries?.token?.status || (loading ? "Loading" : "Unknown"),
        badge: statusToBadge(healthEntries?.token?.status),
        caption: healthEntries?.token?.description || "Token reachability",
      },
      {
        title: "Avg Token Issuance Time",
        value: "248 ms",
        badge: "secondary",
        caption: "Last 60 minutes",
      },
      {
        title: "Error Rate (15 min)",
        value: "0.9%",
        badge: "danger",
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

    const spike = emptyDashboard.tokenVolumeSpike;
    const spikeDimension = spike?.dimension ?? spike?.Dimension ?? "Unknown client";
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
      topClients,
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
    };
  }, [normalizedDashboard, health, loading, error]);

  return (
    <>
      <DashboardHeader lastUpdatedLabel={viewModel.lastUpdatedLabel} />

      <KpiRow kpis={viewModel.kpis} />

      <div className="row g-3 my-2">
        <div className="col-12">
          <SecurityAlerts alerts={viewModel.alerts} />
        </div>
      </div>

      <div className="dashboard-auth-layout my-3">
        <div className="dashboard-auth-main">
          <AuthenticationActivity
            series={viewModel.authSeries}
            totals={viewModel.totals}
          />
        </div>

        <div className="dashboard-summary-panel">
          <div className="card-lite h-100 dashboard-summary-card">
            <div className="card-header">
              <div>
                <h6 className="mb-0">Authentication Summary</h6>
                <div className="text-muted small">
                  Token issuance and login counters from the last 24 hours
                </div>
              </div>
            </div>
            <div className="card-body">
              <div className="dashboard-summary-grid">
                {viewModel.metricCards.map((metric) => (
                  <div key={metric.label} className="dashboard-summary-metric">
                    <div className="dashboard-summary-label">{metric.label}</div>
                    <div className="dashboard-summary-value">
                      {loading && !normalizedDashboard ? "--" : metric.value}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="row g-3 my-2">
        <div className="col-12">
          <TopClientsVolume topClients={viewModel.topClients} />
        </div>
      </div>
    </>
  );
}

export default Dashboard;
