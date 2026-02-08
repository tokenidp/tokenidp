import React from "react";

const logs = [
  { timestamp: "04/24/2024", event: "Token revoked", user: "System" },
  { timestamp: "04/24/2024", event: "Policy updated", user: "Admin" },
];

function SystemLogsTable() {
  return (
    <div className="card-lite">
      <div className="card-header d-flex justify-content-between">
        <strong>System Logs</strong>
        <a href="#!" className="small text-decoration-none">
          View all
        </a>
      </div>
      <div className="card-body">
        <table className="table table-sm mb-0">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>Event</th>
              <th className="text-end">User</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log, index) => (
              <tr key={`${log.timestamp}-${index}`}>
                <td>{log.timestamp}</td>
                <td>{log.event}</td>
                <td className="text-end">{log.user}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default SystemLogsTable;
