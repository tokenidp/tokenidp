import React from "react";

function SecurityAlerts({ alerts }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header d-flex justify-content-between align-items-center">
        <div>
          <h6 className="mb-0">Security Alerts</h6>
          <div className="text-muted small">High-signal indicators only</div>
        </div>
        <span className="badge bg-light text-dark">{alerts.length} alerts</span>
      </div>
      <div className="card-body">
        <div className="row g-3">
          {alerts.map((alert) => (
            <div key={alert.title} className="col-12 col-lg-4">
              <div
                className={`border rounded p-3 h-100 bg-${alert.level} bg-opacity-10`}
              >
                <div className="d-flex justify-content-between align-items-start">
                  <strong>{alert.title}</strong>
                  <span className={`badge bg-${alert.level}`}>{alert.level}</span>
                </div>
                <div className="text-muted small mt-2">{alert.detail}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default SecurityAlerts;
