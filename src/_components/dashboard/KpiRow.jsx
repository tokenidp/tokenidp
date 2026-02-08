import React from "react";

function KpiRow({ kpis }) {
  return (
    <div className="row row-cols-1 row-cols-sm-2 row-cols-lg-3 row-cols-xxl-5 g-3 my-2">
      {kpis.map((metric) => (
        <div key={metric.title} className="col">
          <div
            className={`metric-card h-100 ${
              metric.badge === "success" ? "metric-card-success" : `bg-${metric.badge} bg-opacity-10`
            }`}
          >
            <div className="d-flex justify-content-between align-items-start">
              <div className="text-secondary small">{metric.title}</div>
              <span className={`badge bg-${metric.badge}`}>{metric.value}</span>
            </div>
            <div className="mt-2 text-muted small">{metric.caption}</div>
          </div>
        </div>
      ))}
    </div>
  );
}

export default KpiRow;
