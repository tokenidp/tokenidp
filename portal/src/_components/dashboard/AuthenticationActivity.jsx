import React, { useMemo } from "react";

const CHART_WIDTH = 720;
const CHART_HEIGHT = 320;
const CHART_MARGIN = { top: 18, right: 18, bottom: 42, left: 54 };
const FALLBACK_POINT_COUNT = 6;

const formatCompactNumber = (value) => {
  const numericValue = Number(value) || 0;
  if (numericValue >= 1000) {
    return `${(numericValue / 1000).toFixed(numericValue >= 10000 ? 0 : 1)}k`;
  }
  return String(Math.round(numericValue));
};

const formatNumber = (value) => {
  const numericValue = Number(value) || 0;
  return new Intl.NumberFormat().format(numericValue);
};

const formatHourLabel = (timestamp) =>
  new Intl.DateTimeFormat([], {
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  }).format(new Date(timestamp));

const getTimestamp = (point) => {
  const rawValue =
    point?.timestamp ??
    point?.Timestamp ??
    point?.timeStamp ??
    point?.TimeStamp ??
    point?.dateTime ??
    point?.DateTime ??
    point?.date ??
    point?.Date ??
    point?.period ??
    point?.Period ??
    point?.label ??
    point?.Label;

  if (rawValue === null || rawValue === undefined) {
    return null;
  }

  const parsed = new Date(rawValue).getTime();
  return Number.isFinite(parsed) ? parsed : null;
};

const getNumeric = (value) => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
};

const getStatusName = (point) =>
  String(
    point?.series ??
      point?.Series ??
      point?.status ??
      point?.Status ??
      point?.type ??
      point?.Type ??
      point?.dimension ??
      point?.Dimension ??
      point?.name ??
      point?.Name ??
      "",
  ).toLowerCase();

const extractInlineCounts = (point) => {
  const success =
    getNumeric(point?.successful) ??
    getNumeric(point?.Successful) ??
    getNumeric(point?.successfulLogins) ??
    getNumeric(point?.SuccessfulLogins) ??
    getNumeric(point?.successCount) ??
    getNumeric(point?.SuccessCount) ??
    getNumeric(point?.success) ??
    getNumeric(point?.Success);

  const failed =
    getNumeric(point?.failed) ??
    getNumeric(point?.Failed) ??
    getNumeric(point?.failedLogins) ??
    getNumeric(point?.FailedLogins) ??
    getNumeric(point?.failedCount) ??
    getNumeric(point?.FailedCount) ??
    getNumeric(point?.failure) ??
    getNumeric(point?.Failure);

  return { success, failed };
};

const buildSeriesFromRows = (rows, totals) => {
  if (!rows.length) {
    return [];
  }

  const hasInlineCounts = rows.some(
    (row) => row.success !== null || row.failed !== null,
  );
  if (hasInlineCounts) {
    return rows.map((row) => ({
      timestamp: row.timestamp,
      success: row.success ?? 0,
      failed: row.failed ?? 0,
    }));
  }

  const grouped = new Map();

  rows.forEach((row) => {
    const key = row.timestamp ?? row.index;
    if (!grouped.has(key)) {
      grouped.set(key, {
        timestamp: row.timestamp,
        success: 0,
        failed: 0,
        total: 0,
      });
    }

    const entry = grouped.get(key);
    const statusName = row.statusName;
    const numericValue = row.value ?? 0;

    if (
      statusName.includes("fail") ||
      statusName.includes("error") ||
      statusName.includes("deny")
    ) {
      entry.failed += numericValue;
      return;
    }

    if (statusName.includes("success") || statusName.includes("pass")) {
      entry.success += numericValue;
      return;
    }

    entry.total += numericValue;
  });

  const groupedRows = Array.from(grouped.values()).sort(
    (a, b) => (a.timestamp ?? 0) - (b.timestamp ?? 0),
  );

  const hasExplicitSplit = groupedRows.some(
    (row) => row.success > 0 || row.failed > 0,
  );
  if (hasExplicitSplit) {
    return groupedRows.map((row) => ({
      timestamp: row.timestamp,
      success: row.success,
      failed: row.failed,
    }));
  }

  const successTotal = Number(totals?.successfulLogins) || 0;
  const failedTotal = Number(totals?.failedLogins) || 0;
  const attemptsTotal = successTotal + failedTotal;
  const successRatio = attemptsTotal > 0 ? successTotal / attemptsTotal : 0.82;
  const failedRatio = attemptsTotal > 0 ? failedTotal / attemptsTotal : 0.18;

  return groupedRows.map((row) => ({
    timestamp: row.timestamp,
    success: Math.round(row.total * successRatio),
    failed: Math.round(row.total * failedRatio),
  }));
};

const buildFallbackSeries = () => {
  const now = Date.now();
  return Array.from({ length: FALLBACK_POINT_COUNT }, (_, index) => ({
    timestamp: now - (FALLBACK_POINT_COUNT - 1 - index) * 4 * 60 * 60 * 1000,
    success: 0,
    failed: 0,
  }));
};

const normalizeActivitySeries = (series, totals) => {
  const rows = (series || [])
    .map((point, index) => {
      const inlineCounts = extractInlineCounts(point);
      return {
        index,
        timestamp: getTimestamp(point),
        statusName: getStatusName(point),
        value:
          getNumeric(point?.value) ??
          getNumeric(point?.Value) ??
          getNumeric(point?.count) ??
          getNumeric(point?.Count) ??
          getNumeric(point?.total) ??
          getNumeric(point?.Total) ??
          0,
        success: inlineCounts.success,
        failed: inlineCounts.failed,
      };
    })
    .filter(
      (row) =>
        row.timestamp !== null ||
        row.success !== null ||
        row.failed !== null ||
        row.value > 0,
    );

  const builtSeries = buildSeriesFromRows(rows, totals);
  const normalized = builtSeries
    .map((point, index) => ({
      ...point,
      timestamp:
        point.timestamp ??
        Date.now() - (builtSeries.length - 1 - index) * 4 * 60 * 60 * 1000,
    }))
    .sort((a, b) => a.timestamp - b.timestamp);

  return normalized.length ? normalized : buildFallbackSeries();
};

const createLinePath = (points) => {
  if (points.length < 2) {
    return "";
  }

  let path = `M ${points[0].x} ${points[0].y}`;
  for (let index = 0; index < points.length - 1; index += 1) {
    const previous = points[index - 1] || points[index];
    const current = points[index];
    const next = points[index + 1];
    const afterNext = points[index + 2] || next;

    const cp1x = current.x + (next.x - previous.x) / 6;
    const cp1y = current.y + (next.y - previous.y) / 6;
    const cp2x = next.x - (afterNext.x - current.x) / 6;
    const cp2y = next.y - (afterNext.y - current.y) / 6;

    path += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${next.x} ${next.y}`;
  }

  return path;
};

const createAreaPath = (points, baselineY) => {
  const linePath = createLinePath(points);
  if (!linePath) {
    return "";
  }

  const first = points[0];
  const last = points[points.length - 1];
  return `${linePath} L ${last.x} ${baselineY} L ${first.x} ${baselineY} Z`;
};

function AuthenticationActivity({ series, totals }) {
  const normalizedSeries = useMemo(
    () => normalizeActivitySeries(series, totals),
    [series, totals],
  );

  const summaryStats = useMemo(() => {
    const successful = Number(totals?.successfulLogins) || 0;
    const failed = Number(totals?.failedLogins) || 0;
    const total = successful + failed;
    const rate = total > 0 ? ((successful / total) * 100).toFixed(2) : "0.00";
    return { successful, failed, total, rate };
  }, [totals]);

  const chart = useMemo(() => {
    const innerWidth = CHART_WIDTH - CHART_MARGIN.left - CHART_MARGIN.right;
    const innerHeight = CHART_HEIGHT - CHART_MARGIN.top - CHART_MARGIN.bottom;
    const timestamps = normalizedSeries.map((point) => point.timestamp);
    const minTimestamp = Math.min(...timestamps);
    const maxTimestamp = Math.max(...timestamps);
    const totalRange = Math.max(maxTimestamp - minTimestamp, 1);

    const maxValue = Math.max(
      500,
      ...normalizedSeries.map((point) => point.success + point.failed),
    );
    // Round up to nearest 500 for clean y-axis ticks
    const yMax = Math.ceil(maxValue / 500) * 500;

    const toX = (timestamp) =>
      CHART_MARGIN.left +
      ((timestamp - minTimestamp) / totalRange) * innerWidth;
    const toY = (value) =>
      CHART_MARGIN.top + innerHeight - (value / yMax) * innerHeight;

    // Single combined total line
    const totalPoints = normalizedSeries.map((point) => ({
      x: toX(point.timestamp),
      y: toY(point.success + point.failed),
      value: point.success + point.failed,
      timestamp: point.timestamp,
    }));

    // Y ticks: evenly spaced from 0 to yMax (5 ticks including 0)
    const tickCount = 5;
    const yTicks = Array.from({ length: tickCount }, (_, index) => {
      const value = Math.round(
        (yMax / (tickCount - 1)) * (tickCount - 1 - index),
      );
      return {
        value,
        y: toY(value),
      };
    });

    // X ticks: 7 labels across the 24-hour range
    const xTicks = Array.from({ length: 7 }, (_, index) => {
      const timestamp = minTimestamp + (totalRange / 6) * index;
      return {
        label: formatHourLabel(timestamp),
        x: toX(timestamp),
      };
    });

    return {
      totalLinePath: createLinePath(totalPoints),
      totalAreaPath: createAreaPath(
        totalPoints,
        CHART_MARGIN.top + innerHeight,
      ),
      yTicks,
      xTicks,
      totalPoints,
    };
  }, [normalizedSeries]);

  return (
    <div className="card-lite h-100 dashboard-auth-card">
      <div className="card-header dashboard-auth-header">
        <div className="dashboard-auth-header-left">
          <div className="dashboard-auth-title-row">
            <span className="dashboard-auth-icon">
              <svg
                width="20"
                height="20"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
              </svg>
            </span>
            <div>
              <h6 className="mb-0">
                Authentication Activity
                <span className="dashboard-auth-period"> (Last 24 Hours)</span>
              </h6>
              <div className="dashboard-auth-sub text-muted small">
                Success Rate: {summaryStats.rate}%{" "}
                <span className="dashboard-auth-sub-divider">•</span> Total:{" "}
                {formatNumber(summaryStats.total)} attempts
              </div>
            </div>
          </div>
        </div>
        <div className="dashboard-auth-stats">
          <span className="dashboard-auth-stat">
            <span className="dashboard-auth-stat-dot is-success" />
            Successful{" "}
            <strong className="dashboard-auth-stat-value">
              {formatNumber(summaryStats.successful)}
            </strong>
          </span>
          <span className="dashboard-auth-stat">
            <span className="dashboard-auth-stat-dot is-failed" />
            Failed{" "}
            <strong className="dashboard-auth-stat-value">
              {formatNumber(summaryStats.failed)}
            </strong>
          </span>
        </div>
      </div>
      <div className="card-body">
        <div className="dashboard-auth-chart-shell">
          <svg
            viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`}
            width="100%"
            height="100%"
            preserveAspectRatio="none"
            className="dashboard-auth-chart"
            aria-label="Authentication activity chart"
          >
            <defs>
              <linearGradient id="auth-total-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.22" />
                <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.03" />
              </linearGradient>
            </defs>

            {chart.yTicks.map((tick) => (
              <g key={`y-${tick.value}`}>
                <line
                  x1={CHART_MARGIN.left}
                  y1={tick.y}
                  x2={CHART_WIDTH - CHART_MARGIN.right}
                  y2={tick.y}
                  className="dashboard-auth-grid-line"
                />
                <text
                  x={CHART_MARGIN.left - 12}
                  y={tick.y + 4}
                  textAnchor="end"
                  className="dashboard-auth-axis-label"
                >
                  {formatCompactNumber(tick.value)}
                </text>
              </g>
            ))}

            {chart.xTicks.map((tick) => (
              <text
                key={`x-${tick.label}-${tick.x}`}
                x={tick.x}
                y={CHART_HEIGHT - 12}
                textAnchor="middle"
                className="dashboard-auth-axis-label"
              >
                {tick.label}
              </text>
            ))}

            {chart.totalAreaPath && (
              <path d={chart.totalAreaPath} fill="url(#auth-total-fill)" />
            )}

            {chart.totalLinePath && (
              <path
                d={chart.totalLinePath}
                fill="none"
                stroke="#3b82f6"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            )}
          </svg>
        </div>
      </div>
    </div>
  );
}

export default AuthenticationActivity;