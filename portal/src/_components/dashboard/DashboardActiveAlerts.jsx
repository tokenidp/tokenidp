import React from "react";

const getAlertCardIcon = (type) => {
  switch (type) {
    case "shield":
      return (
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M12 2 4 5v6c0 5.55 3.84 10.74 8 11 4.16-.26 8-5.45 8-11V5l-8-3Z"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path
            d="m9 12 2 2 4-4"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      );
    case "clock":
      return (
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2" />
          <path
            d="M12 7v5l3 3"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      );
    case "alert":
      return (
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M12 9v4"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
          />
          <path
            d="M12 17h.01"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
          />
          <path
            d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      );
    default:
      return null;
  }
};

function DashboardActiveAlerts({ activeAlertCount, alertCards }) {
  return (
    <div className="dashboard-active-alerts-section mb-3">
      <div className="dashboard-active-alerts-heading">
        <span>Active Alerts</span>
        {activeAlertCount > 0 && (
          <span className="dashboard-active-alerts-badge">
            {activeAlertCount}
          </span>
        )}
      </div>
      <div className="dashboard-alert-card-grid">
        {alertCards.map((card) => (
          <div
            key={card.title}
            className={`dashboard-alert-card card-lite dashboard-alert-card-${card.status}`}
          >
            <div className="dashboard-alert-card-body">
              <div className="dashboard-alert-card-icon-wrap">
                <div
                  className={`dashboard-alert-card-icon dashboard-alert-card-icon-${card.status}`}
                >
                  {getAlertCardIcon(card.icon)}
                </div>
                <div className="dashboard-alert-card-copy">
                  <div className="dashboard-alert-card-title">{card.title}</div>
                  <div className="dashboard-alert-card-footer">
                    <span
                      className={`dashboard-alert-card-pill dashboard-alert-card-pill-${card.status}`}
                    >
                      {card.badgeText}
                    </span>
                    <span className="dashboard-alert-card-meta">
                      {card.meta}
                    </span>
                  </div>
                </div>
              </div>
              <div className="dashboard-alert-card-detail">{card.detail}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default DashboardActiveAlerts;
