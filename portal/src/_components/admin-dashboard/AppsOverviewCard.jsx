import React from "react";

function AppsOverviewCard({ appStats, expiringApps }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header">
        <h6 className="mb-0">Applications / Clients Overview</h6>
        <div className="text-muted small">Active apps and token usage</div>
      </div>
      <div className="card-body">
        <div className="row g-2 mb-3">
          {appStats.map((stat) => (
            <div key={stat.label} className="col-12">
              <div className="d-flex justify-content-between border rounded px-3 py-2">
                <span className="text-muted small">{stat.label}</span>
                <strong>{stat.value}</strong>
              </div>
            </div>
          ))}
        </div>
        <div className="d-flex justify-content-between align-items-center mb-2">
          <span className="text-muted small">Apps with expiring secrets</span>
          <span className="text-muted small">Expires</span>
        </div>
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Application</th>
                <th className="text-end">Expiry</th>
              </tr>
            </thead>
            <tbody>
              {expiringApps.map((app) => (
                <tr key={app.app}>
                  <td>{app.app}</td>
                  <td className="text-end">{app.expires}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default AppsOverviewCard;
