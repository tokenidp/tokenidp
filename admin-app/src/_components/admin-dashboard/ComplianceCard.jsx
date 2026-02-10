import React from "react";

function ComplianceCard({ complianceStats }) {
  return (
    <div className="card-lite">
      <div className="card-header">
        <h6 className="mb-0">Compliance &amp; Hygiene</h6>
        <div className="text-muted small">Operational readiness signals</div>
      </div>
      <div className="card-body">
        <div className="row g-3">
          {complianceStats.map((stat) => (
            <div key={stat.label} className="col-12 col-md-6 col-xl-3">
              <div className="border rounded px-3 py-3 h-100">
                <div className="text-secondary small">{stat.label}</div>
                <div className="fs-5 fw-semibold">{stat.value}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default ComplianceCard;
