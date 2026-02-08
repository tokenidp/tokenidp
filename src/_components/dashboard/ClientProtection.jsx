import React from "react";

function ClientProtection({ protectionStats, noisyClients }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header d-flex justify-content-between align-items-center">
        <div>
          <h6 className="mb-0">Client Protection &amp; Abuse</h6>
          <div className="text-muted small">Rate limits, queues, and noisy clients</div>
        </div>
      </div>
      <div className="card-body">
        <div className="row g-2 mb-3">
          {protectionStats.map((stat) => (
            <div key={stat.label} className="col-12">
              <div className="d-flex justify-content-between border rounded px-3 py-2">
                <span className="text-muted small">{stat.label}</span>
                <strong>{stat.value}</strong>
              </div>
            </div>
          ))}
        </div>
        <div className="d-flex justify-content-between align-items-center mb-2">
          <span className="text-muted small">Noisiest clients</span>
          <span className="text-muted small">Events</span>
        </div>
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Client</th>
                <th>Action</th>
                <th className="text-end">Events</th>
              </tr>
            </thead>
            <tbody>
              {noisyClients.map((client) => (
                <tr key={client.name}>
                  <td>{client.name}</td>
                  <td className="text-muted small">{client.action}</td>
                  <td className="text-end">{client.events}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default ClientProtection;
