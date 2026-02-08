import React from "react";

function AccessUsageCard({ accessStats, permissionTrends }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header">
        <h6 className="mb-0">Access &amp; Permission Usage</h6>
        <div className="text-muted small">How access is used across roles</div>
      </div>
      <div className="card-body">
        <div className="row g-2 mb-3">
          {accessStats.map((stat) => (
            <div key={stat.label} className="col-12">
              <div className="d-flex justify-content-between border rounded px-3 py-2">
                <span className="text-muted small">{stat.label}</span>
                <strong>{stat.value}</strong>
              </div>
            </div>
          ))}
        </div>
        <div className="d-flex justify-content-between align-items-center mb-2">
          <span className="text-muted small">Top used permissions</span>
          <span className="text-muted small">Usage</span>
        </div>
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Permission</th>
                <th className="text-end">Usage</th>
              </tr>
            </thead>
            <tbody>
              {permissionTrends.map((row) => (
                <tr key={row.permission}>
                  <td>{row.permission}</td>
                  <td className="text-end">{row.usage}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default AccessUsageCard;
