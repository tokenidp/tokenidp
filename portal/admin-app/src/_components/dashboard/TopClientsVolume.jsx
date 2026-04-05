import React from "react";

function TopClientsVolume({ topClients }) {
  return (
    <div className="card-lite h-100">
      <div className="card-header d-flex justify-content-between align-items-center">
        <div>
          <h6 className="mb-0">Top clients by volume</h6>
          <div className="text-muted small">Token issuance by client</div>
        </div>
        <span className="badge bg-light text-dark">Last 24h</span>
      </div>
      <div className="card-body">
        <div className="table-responsive">
          <table className="table table-sm mb-0 table-striped table-bordered">
            <thead className="tbl-heading-gradient">
              <tr>
                <th>Client</th>
                <th>Grant Type</th>
                <th className="text-end">Tokens</th>
              </tr>
            </thead>
            <tbody>
              {topClients.map((client) => (
                <tr key={client.clientId || client.name}>
                  <td>{client.name}</td>
                  <td className="text-muted small">{client.grant}</td>
                  <td className="text-end">{client.tokens}</td>
                </tr>
              ))}
              {topClients.length === 0 && (
                <tr>
                  <td colSpan="3" className="text-center text-muted py-3">
                    No client volume data.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default TopClientsVolume;