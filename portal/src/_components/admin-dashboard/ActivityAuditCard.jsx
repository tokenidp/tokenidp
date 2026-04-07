import React from "react";

function ActivityAuditCard({ auditEvents }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header">
        <h6 className="mb-0">Activity &amp; Audit Summary</h6>
        <div className="text-muted small">Recent administrative actions</div>
      </div>
      <div className="card-body">
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Event</th>
                <th>Actor</th>
                <th className="text-end">Time</th>
              </tr>
            </thead>
            <tbody>
              {auditEvents.map((event) => (
                <tr key={`${event.event}-${event.actor}`}>
                  <td>{event.event}</td>
                  <td className="text-muted small">{event.actor}</td>
                  <td className="text-end">{event.time}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="text-muted small mt-2">
          Showing the latest administrative changes across the tenant.
        </div>
      </div>
    </div>
  );
}

export default ActivityAuditCard;
