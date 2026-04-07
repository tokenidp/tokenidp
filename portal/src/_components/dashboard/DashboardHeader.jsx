import React from "react";

function DashboardHeader({ lastUpdatedLabel }) {
  return (
    <div className="navbar d-flex justify-content-between align-items-center">
      <div>
        <h5 className="mb-0">IDP Security Dashboard</h5>
        <div className="text-muted small">
          Monitor authentication activity, token issuance, and sign-in outcomes.
        </div>
      </div>
      <div className="text-muted small">
        {lastUpdatedLabel || "Last updated: --"}
      </div>
    </div>
  );
}

export default DashboardHeader;
