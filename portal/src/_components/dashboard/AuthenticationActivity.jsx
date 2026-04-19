import React, { useMemo } from "react";

const CHART_WIDTH = 720;
const CHART_HEIGHT = 320;
const CHART_MARGIN = { top: 18, right: 18, bottom: 42, left: 54 };
const HOUR_MS = 60 * 60 * 1000;
const DAY_MS = 24 * HOUR_MS;
const PERIOD_CONFIG = {
  daily: { pointCount: 24, bucketMs: HOUR_MS, label: "Last 24 Hours" },
  weekly: { pointCount: 7, bucketMs: DAY_MS, label: "Last 7 Days" },
  monthly: { pointCount: 30, bucketMs: DAY_MS, label: "Last 30 Days" },
};

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

const formatDayLabel = (timestamp) =>
  new Intl.DateTimeFormat([], {
    month: "short",
    day: "numeric",
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

const getPeriodConfig = (period) => PERIOD_CONFIG[period] || PERIOD_CONFIG.daily;

const floorToHour = (timestamp) => {
  const date = new Date(timestamp);
  date.setMinutes(0, 0, 0);
  return date.getTime();
};

const floorToDay = (timestamp) => {
  const date = new Date(timestamp);
  date.setHours(0, 0, 0, 0);
  return date.getTime();
};

const floorToBucket = (timestamp, period) =>
  period === "daily" ? floorToHour(timestamp) : floorToDay(timestamp);

const densifySeries = (series, period) => {
  if (!series.length) {
    return [];
  }

  const config = getPeriodConfig(period);
  const grouped = new Map();

  series.forEach((point) => {
    const bucket = floorToBucket(point.timestamp, period);
    const current = grouped.get(bucket) ?? {
      timestamp: bucket,
      success: 0,
      failed: 0,
    };

    current.success += Number(point.success) || 0;
    current.failed += Number(point.failed) || 0;
    grouped.set(bucket, current);
  });

  const maxTimestamp = Math.max(...Array.from(grouped.keys()));
  const endBucket = floorToBucket(maxTimestamp || Date.now(), period);
  const startBucket = endBucket - (config.pointCount - 1) * config.bucketMs;

  return Array.from({ length: config.pointCount }, (_, index) => {
    const timestamp = startBucket + index * config.bucketMs;
    const point = grouped.get(timestamp);
    return point ?? { timestamp, success: 0, failed: 0 };
  });
};

const buildFallbackSeries = (period) => {
  const config = getPeriodConfig(period);
  const endBucket = floorToBucket(Date.now(), period);
  const startBucket = endBucket - (config.pointCount - 1) * config.bucketMs;
  return Array.from({ length: config.pointCount }, (_, index) => ({
    timestamp: startBucket + index * config.bucketMs,
    success: 0,
    failed: 0,
  }));
};

const normalizeActivitySeries = (series, totals, period) => {
  const config = getPeriodConfig(period);
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
        Date.now() - (builtSeries.length - 1 - index) * config.bucketMs,
    }))
    .sort((a, b) => a.timestamp - b.timestamp);

  return normalized.length ? densifySeries(normalized, period) : buildFallbackSeries(period);
};

const roundUpYAxis = (value) => {
  if (value <= 10) return 10;
  if (value <= 50) return Math.ceil(value / 5) * 5;
  if (value <= 100) return Math.ceil(value / 10) * 10;
  if (value <= 500) return Math.ceil(value / 50) * 50;
  return Math.ceil(value / 100) * 100;
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

function AuthenticationActivity({ series, totals, period, periodLabel }) {
  const normalizedSeries = useMemo(
    () => normalizeActivitySeries(series, totals, period),
    [series, totals, period],
  );

  const summaryStats = useMemo(() => {
    const successful = Number(totals?.successfulLogins) || 0;
    const failed = Number(totals?.failedLogins) || 0;
    const total = successful + failed;
    const rate = total > 0 ? ((successful / total) * 100).toFixed(2) : "0.00";
    return { successful, failed, total, rate };
  }, [totals]);

  const chart = useMemo(() => {
    const isDaily = period === "daily";
    const innerWidth = CHART_WIDTH - CHART_MARGIN.left - CHART_MARGIN.right;
    const innerHeight = CHART_HEIGHT - CHART_MARGIN.top - CHART_MARGIN.bottom;
    const timestamps = normalizedSeries.map((point) => point.timestamp);
    const minTimestamp = Math.min(...timestamps);
    const maxTimestamp = Math.max(...timestamps);
    const totalRange = Math.max(maxTimestamp - minTimestamp, 1);

    const maxValue = Math.max(
      1,
      ...normalizedSeries.flatMap((point) => [point.success, point.failed]),
    );
    const yMax = roundUpYAxis(maxValue);

    const toX = (timestamp) =>
      CHART_MARGIN.left +
      ((timestamp - minTimestamp) / totalRange) * innerWidth;
    const toY = (value) =>
      CHART_MARGIN.top + innerHeight - (value / yMax) * innerHeight;

    const successPoints = normalizedSeries.map((point) => ({
      x: toX(point.timestamp),
      y: toY(point.success),
      value: point.success,
      timestamp: point.timestamp,
    }));

    const failedPoints = normalizedSeries.map((point) => ({
      x: toX(point.timestamp),
      y: toY(point.failed),
      value: point.failed,
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

    const xTickCount = isDaily ? 7 : period === "monthly" ? 6 : 7;
    const xTicks = Array.from({ length: xTickCount }, (_, index) => {
      const divisor = Math.max(xTickCount - 1, 1);
      const timestamp = minTimestamp + (totalRange / divisor) * index;
      return {
        label: isDaily ? formatHourLabel(timestamp) : formatDayLabel(timestamp),
        x: toX(timestamp),
      };
    });

    return {
      successLinePath: createLinePath(successPoints),
      successAreaPath: createAreaPath(
        successPoints,
        CHART_MARGIN.top + innerHeight,
      ),
      failedLinePath: createLinePath(failedPoints),
      failedAreaPath: createAreaPath(
        failedPoints,
        CHART_MARGIN.top + innerHeight,
      ),
      yTicks,
      xTicks,
      successPoints,
      failedPoints,
    };
  }, [normalizedSeries, period]);

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
                <span className="dashboard-auth-period"> ({periodLabel || getPeriodConfig(period).label})</span>
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
        <div className="dashboard-auth-legend">
          <span className="dashboard-auth-legend-item">
            <span className="dashboard-auth-legend-swatch is-success" />
            Successful
          </span>
          <span className="dashboard-auth-legend-item">
            <span className="dashboard-auth-legend-swatch is-failed" />
            Failed
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
              <linearGradient id="auth-success-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#22c55e" stopOpacity="0.18" />
                <stop offset="100%" stopColor="#22c55e" stopOpacity="0.03" />
              </linearGradient>
              <linearGradient id="auth-failed-fill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#ef4444" stopOpacity="0.16" />
                <stop offset="100%" stopColor="#ef4444" stopOpacity="0.02" />
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

            {chart.successAreaPath && (
              <path d={chart.successAreaPath} fill="url(#auth-success-fill)" />
            )}

            {chart.failedAreaPath && (
              <path d={chart.failedAreaPath} fill="url(#auth-failed-fill)" />
            )}

            {chart.successLinePath && (
              <path
                d={chart.successLinePath}
                fill="none"
                stroke="#22c55e"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            )}

            {chart.failedLinePath && (
              <path
                d={chart.failedLinePath}
                fill="none"
                stroke="#ef4444"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeDasharray="6 4"
              />
            )}
          </svg>
        </div>
      </div>
    </div>
  );
}

export default AuthenticationActivity;
