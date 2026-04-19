import React from "react";

function DashboardTechnicalDetails({ technicalDetails }) {
  return (
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
              className={`dashboard-system-status-dot ${technicalDetails.allOperational ? "success" : "warning"}`}
            />
            <div>
              <div className="fw-semibold" style={{ fontSize: "13px" }}>
                IDP Service Status
              </div>
              <div
                className={
                  technicalDetails.allOperational
                    ? "text-success"
                    : "text-warning"
                }
                style={{ fontSize: "12px" }}
              >
                {technicalDetails.allOperational
                  ? "All endpoints operational"
                  : "Some endpoints degraded"}
              </div>
            </div>
          </div>
          <div className="dashboard-tech-meta-grid">
            {technicalDetails.metaItems.map((item) => (
              <div key={item.label} className="dashboard-tech-meta-item">
                <span className="dashboard-tech-meta-label">{item.label}</span>
                <span className="dashboard-tech-meta-value">{item.value}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="dashboard-tech-info-grid">
          {technicalDetails.infoItems.map((item) => (
            <div key={item.label} className="dashboard-tech-info-row">
              <span className="dashboard-tech-info-label">{item.label}</span>
              <span className="dashboard-tech-info-value">{item.value}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default DashboardTechnicalDetails;
