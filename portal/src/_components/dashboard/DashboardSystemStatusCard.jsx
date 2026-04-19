import React from "react";

function DashboardSystemStatusCard({ systemStatus }) {
  return (
    <div className="dashboard-system-status-card card-lite mb-3">
      <div className="dashboard-system-status-card-body">
        <div className="dashboard-system-status-card-main">
          <div className="dashboard-system-status-card-icon">
            <svg
              width="24"
              height="24"
              viewBox="0 0 24 24"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <circle
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="2"
              />
              <path
                d="M8 12.5L10.5 15L16 9"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </div>
          <div className="dashboard-system-status-card-info">
            <div className="dashboard-system-status-card-title-row">
              <h5 className="dashboard-system-status-card-title">
                {systemStatus.title}
              </h5>
              <span
                className={`status-pill status-pill-${systemStatus.liveStatus} dashboard-system-status-live`}
              >
                <svg
                  width="14"
                  height="14"
                  viewBox="0 0 24 24"
                  fill="none"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    d="M4 12h4l2 5 3-10 2 5h5"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
                {systemStatus.liveLabel}
              </span>
            </div>
            <div className="dashboard-system-status-card-caption">
              {systemStatus.caption}
            </div>
          </div>
        </div>
        <div className="dashboard-system-status-items">
          {systemStatus.items.map((item) => (
            <div key={item.label} className="dashboard-system-status-item">
              <span className={`dashboard-system-status-dot ${item.status}`} />
              <div>
                <div className="dashboard-system-status-item-label">
                  {item.label}
                </div>
                <div className="dashboard-system-status-item-value">
                  {item.value}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default DashboardSystemStatusCard;
