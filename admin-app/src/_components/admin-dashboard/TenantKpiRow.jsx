import React from "react";

function TenantKpiRow({ kpis }) {
  return (
    <div className="row g-3 my-3">
      {kpis.map((stat) => (
        <div key={stat.label} className="col-12 col-md-6 col-xl-3">
          <div className="metric-card h-100">
            <div className="text-secondary small">{stat.label}</div>
            <div className="value">{stat.value}</div>
          </div>
        </div>
      ))}
    </div>
  );
}

export default TenantKpiRow;
