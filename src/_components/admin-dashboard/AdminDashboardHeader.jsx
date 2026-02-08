import React from "react";

function AdminDashboardHeader() {
  return (
    <div className="navbar d-flex justify-content-between align-items-center">
      <div>
        <h5 className="mb-0">Tenant Admin Dashboard</h5>
        <div className="text-muted small">
          Monitor tenant users, roles, applications, and access usage.
        </div>
      </div>
      <div className="text-muted small">Last updated: 10 minutes ago</div>
    </div>
  );
}

export default AdminDashboardHeader;
