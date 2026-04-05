import React from "react";

function LoginActivityChart() {
  return (
    <div className="card-lite mb-3">
      <div className="card-header d-flex justify-content-between">
        <strong>Login Activity</strong>
        <a href="#!" className="small text-decoration-none">
          View all
        </a>
      </div>
      <div className="card-body">
        <div className="chart-placeholder">
          <div className="chart-wave"></div>
        </div>
      </div>
    </div>
  );
}

export default LoginActivityChart;
