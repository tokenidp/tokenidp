import React from "react";

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
      {metrics.map((metric, index) => (
        <React.Fragment key={metric.label}>
          {index > 0 && <div className="dashboard-kpi-divider" />}
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

function DashboardMetricCards({
  tokenMetrics,
  loginActivity,
  securityMetrics,
  skeleton,
}) {
  return (
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
        metrics={tokenMetrics}
        skeleton={skeleton}
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
        metrics={loginActivity}
        skeleton={skeleton}
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
        metrics={securityMetrics}
        skeleton={skeleton}
      />
    </div>
  );
}

export default DashboardMetricCards;
