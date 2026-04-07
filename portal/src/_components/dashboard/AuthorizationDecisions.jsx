import React from "react";

function AuthorizationDecisions({ authzStats, deniedPermissions }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header d-flex justify-content-between align-items-center">
        <div>
          <h6 className="mb-0">Authorization Decisions</h6>
          <div className="text-muted small">Allow/deny behavior and latency</div>
        </div>
      </div>
      <div className="card-body">
        <div className="row g-2 mb-3">
          {authzStats.map((stat) => (
            <div key={stat.label} className="col-12">
              <div className="d-flex justify-content-between border rounded px-3 py-2">
                <span className="text-muted small">{stat.label}</span>
                <strong>{stat.value}</strong>
              </div>
            </div>
          ))}
        </div>
        <div className="d-flex justify-content-between align-items-center mb-2">
          <span className="text-muted small">Top denied permissions</span>
          <span className="text-muted small">Count</span>
        </div>
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Permission</th>
                <th className="text-end">Denied</th>
              </tr>
            </thead>
            <tbody>
              {deniedPermissions.map((item) => (
                <tr key={item.permission}>
                  <td>{item.permission}</td>
                  <td className="text-end">{item.count}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default AuthorizationDecisions;
