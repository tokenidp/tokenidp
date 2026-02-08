import React, { useMemo } from "react";

const normalizeSeries = (points) =>
  (points || [])
    .map((p) => ({
      value: Number(p?.value),
      timestamp: p?.timestamp ? new Date(p.timestamp).getTime() : null,
    }))
    .filter((p) => Number.isFinite(p.value));

const buildPath = (points, width, height, padding) => {
  if (points.length < 3) return "";
  const sorted = points
    .slice()
    .sort((a, b) =>
      Number.isFinite(a.timestamp) && Number.isFinite(b.timestamp)
        ? a.timestamp - b.timestamp
        : 0
    );
  const min = Math.min(...sorted.map((p) => p.value));
  const max = Math.max(...sorted.map((p) => p.value));
  const baseRange = max - min;
  const step = (width - padding * 2) / Math.max(sorted.length - 1, 1);
  const coords = sorted.map((p, i) => {
    const x = padding + i * step;
    const normalized =
      baseRange === 0 ? 0.5 : (p.value - min) / baseRange;
    const waveNormalized = 0.2 + normalized * 0.6;
    const y =
      padding + (height - padding * 2) * (1 - waveNormalized);
    return { x, y };
  });

  let path = `M${coords[0].x},${coords[0].y}`;
  for (let i = 0; i < coords.length - 1; i += 1) {
    const p0 = coords[i - 1] || coords[i];
    const p1 = coords[i];
    const p2 = coords[i + 1];
    const p3 = coords[i + 2] || p2;

    const cp1x = p1.x + (p2.x - p0.x) / 6;
    const cp1y = p1.y + (p2.y - p0.y) / 6;
    const cp2x = p2.x - (p3.x - p1.x) / 6;
    const cp2y = p2.y - (p3.y - p1.y) / 6;

    path += ` C${cp1x},${cp1y} ${cp2x},${cp2y} ${p2.x},${p2.y}`;
  }
  return path;
};

function AuthenticationActivity({ authStats, series }) {
  const safeSeries = useMemo(() => normalizeSeries(series), [series]);
  const chartPath = useMemo(
    () => buildPath(safeSeries, 320, 140, 8),
    [safeSeries]
  );

  return (
    <div className="card-lite h-100">
      <div className="card-header d-flex justify-content-between align-items-center">
        <div>
          <h6 className="mb-0">Authentication Activity</h6>
          <div className="text-muted small">Login success, failures, and MFA</div>
        </div>
        <span className="badge bg-light text-dark">Last 24h</span>
      </div>
      <div className="card-body">
        <div className="chart-placeholder mb-3">
          {!chartPath && <div className="chart-wave"></div>}
          {chartPath && (
            <svg
              viewBox="0 0 320 140"
              width="100%"
              height="100%"
              preserveAspectRatio="none"
              style={{ position: "absolute", inset: 0, zIndex: 1 }}
            >
              <path
                d={chartPath}
                fill="none"
                stroke="#16a34a"
                strokeWidth="6"
                strokeLinecap="round"
              />
            </svg>
          )}
        </div>
        <div className="row g-2">
          {authStats.map((stat) => (
            <div key={stat.label} className="col-12 col-md-6">
              <div className="d-flex justify-content-between border rounded px-3 py-2">
                <span className="text-muted small">{stat.label}</span>
                <strong>{stat.value}</strong>
              </div>
            </div>
          ))}
        </div>
        <div className="text-muted small mt-2">
          Spike detected at 10:20 UTC; MFA enforcement reduced failures.
        </div>
      </div>
    </div>
  );
}

export default AuthenticationActivity;
