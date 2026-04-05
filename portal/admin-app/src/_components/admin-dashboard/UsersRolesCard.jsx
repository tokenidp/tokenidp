import React from "react";

function UsersRolesCard({ recentUsers }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header">
        <h6 className="mb-0">Users &amp; Roles Overview</h6>
        <div className="text-muted small">Snapshot of tenant membership</div>
      </div>
      <div className="card-body">
        <div className="chart-placeholder mb-3">
          <div className="chart-wave"></div>
        </div>
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Recent user</th>
                <th>Role</th>
                <th className="text-end">Added</th>
              </tr>
            </thead>
            <tbody>
              {recentUsers.map((user) => (
                <tr key={user.name}>
                  <td>{user.name}</td>
                  <td className="text-muted small">{user.role}</td>
                  <td className="text-end">{user.added}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default UsersRolesCard;
