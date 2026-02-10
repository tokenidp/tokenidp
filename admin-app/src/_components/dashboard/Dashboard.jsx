import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import DashboardHeader from "./DashboardHeader";
import KpiRow from "./KpiRow";
import AuthenticationActivity from "./AuthenticationActivity";
import TokenUsage from "./TokenUsage";
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
  if (!value) return null;
    return {
      accessTokenIssued: value.accessTokenIssued ?? value.AccessTokenIssued ?? 0,
      refreshTokenIssued: value.refreshTokenIssued ?? value.RefreshTokenIssued ?? 0,
    tokenIssuanceByGrantType:
      value.tokenIssuanceByGrantType ?? value.TokenIssuanceByGrantType ?? 0,
    totalLoginAttempts: value.totalLoginAttempts ?? value.TotalLoginAttempts ?? 0,
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
    authSummary: value.authSummary ?? value.AuthSummary ?? [],
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
        dashboardResponse?.data?.value || dashboardResponse?.data;
      const healthResult = healthResponse?.data || null;
      console.log("DashboardResponse", dashboardResult);
      console.log("HealthResponse", healthResult);
      console.log("tokensLast24h", dashboardResult?.tokensLast24h || dashboardResult?.TokensLast24h || []);
      console.log("authLast24h", dashboardResult?.authLast24h || dashboardResult?.AuthLast24h || []);
      setDashboard(dashboardResult || null);
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
    if (didLoadRef.current) return;
    didLoadRef.current = true;
    loadDashboard();
  }, [loadDashboard]);

  const normalizedDashboard = useMemo(
    () => normalizeDashboard(dashboard),
    [dashboard]
  );

  const viewModel = useMemo(() => {
    const healthEntries = health?.entries || {};

    const healthKpis = [
      {
        title: "IDP Service Status",
        value: health?.status || (loading ? "Loading" : "Unknown"),
        badge: "success",
        caption: "Overall system health",
      },
      {
        title: "Authorization Endpoint",
        value: healthEntries?.authorization?.status || (loading ? "Loading" : "Unknown"),
        badge: loading
          ? "success"
          : String(healthEntries?.authorization?.status || "")
              .toLowerCase() === "healthy"
          ? "success"
          : "warning",
        caption: healthEntries?.authorization?.description || "Authorization reachability",
      },
      {
        title: "Token Endpoint Status",
        value: healthEntries?.token?.status || (loading ? "Loading" : "Unknown"),
        badge: "success",
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

    if (!normalizedDashboard) {
      return {
        kpis: healthKpis,
        tokenStats: [],
        authStats: [],
        topClients: [],
        alerts: error
          ? [
              {
                title: "Dashboard data unavailable",
                detail: "Unable to load dashboard metrics. Please retry.",
                level: "warning",
              },
            ]
          : [],
      };
    }

    const kpis = healthKpis;

    // Wave/area chart data sources: tokensLast24h, authLast24h
    const tokenStats = [
      { label: "Access tokens issued (24h)", value: formatNumber(normalizedDashboard.accessTokenIssued) },
      { label: "Refresh tokens issued (24h)", value: formatNumber(normalizedDashboard.refreshTokenIssued) },
      {
        label: "Auth code grant share",
        value: `${formatNumber(normalizedDashboard.tokenIssuanceByGrantType)}%`,
      },
    ];

    const authStats = [
      { label: "Total login attempts", value: formatNumber(normalizedDashboard.totalLoginAttempts) },
      { label: "Successful logins", value: formatNumber(normalizedDashboard.successfulLogins) },
      { label: "Failed logins", value: formatNumber(normalizedDashboard.failedLogins) },
      { label: "MFA challenges", value: formatNumber(normalizedDashboard.mfaChallenge) },
      { label: "Account lockouts", value: formatNumber(normalizedDashboard.accountLockout) },
    ];

    const lastUpdatedLabel = toRelativeLabel(normalizedDashboard.lastUpdated);

    // Top clients table: topClients
    const topClients = (normalizedDashboard.topClients || []).map((client) => ({
      clientId: client.clientId,
      name: client.clientName,
      grant: client.grantType,
      tokens: formatNumber(client.tokens),
    }));

    // Security widgets: multipleFailedLogin, failedLoginSpikes
    const spikeSummary = (normalizedDashboard.failedLoginSpikes || [])
      .slice(0, 3)
      .map((spike) => `${spike.dimension}: ${formatNumber(spike.value)}`)
      .join(", ");

    const alerts = [
      {
        title: "Multiple failed login attempts",
        detail: `${formatNumber(normalizedDashboard.multipleFailedLogin)} failures in the last 15 minutes.`,
        level: normalizedDashboard.multipleFailedLogin > 0 ? "danger" : "secondary",
      },
      {
        title: "Expiring client secrets",
        detail: `${formatNumber(normalizedDashboard.expiringClientCount)} confidential clients expire within 7 days.`,
        level: normalizedDashboard.expiringClientCount > 0 ? "warning" : "secondary",
      },
    ];

    const spike = normalizedDashboard.tokenVolumeSpike;
    const spikeDimension =
      spike?.dimension ?? spike?.Dimension ?? "Unknown client";
    const spikeValue = spike?.value ?? spike?.Value ?? 0;

    alerts.push({
      title: "Suspicious activity indicators",
      detail:
        spikeValue > 0
          ? `${spikeDimension} has an unusual token volume spike: ${formatNumber(
              spikeValue
            )} tokens in the current hour.`
          : `${formatNumber(
              normalizedDashboard.suspiciousActivity
            )} suspicious events detected.`,
      level:
        spikeValue > 0 || normalizedDashboard.suspiciousActivity > 0
          ? "danger"
          : "secondary",
    });

    return { kpis, tokenStats, authStats, topClients, alerts, lastUpdatedLabel };
  }, [normalizedDashboard, error, health, loading]);

  return (
    <>
      <DashboardHeader lastUpdatedLabel={viewModel.lastUpdatedLabel} />
      <KpiRow kpis={viewModel.kpis} />

      <div className="row g-3 my-2">
        <div className="col-12">
          <SecurityAlerts alerts={viewModel.alerts} />
        </div>
      </div>

      <div className="row g-3 my-2">
        <div className="col-12 col-lg-6">
          <TokenUsage
            tokenStats={viewModel.tokenStats}
            series={normalizedDashboard?.tokensLast24h || []}
          />
        </div>
        <div className="col-12 col-lg-6">
          <AuthenticationActivity
            authStats={viewModel.authStats}
            series={normalizedDashboard?.authLast24h || []}
          />
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
