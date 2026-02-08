import React from "react";

const tokens = [
  { id: "3ae9160772", user: "asmith", client: "App1", issuedAt: "04/19/2024" },
  { id: "3a881592a3", user: "jsmith", client: "App1", issuedAt: "04/19/2024" },
  { id: "3e8a8a470c", user: "lee", client: "App2", issuedAt: "04/19/2024" },
];

function RecentTokensTable() {
  return (
    <div className="card-lite">
      <div className="card-header d-flex justify-content-between">
        <strong>Recent Tokens</strong>
        <a href="#!" className="small text-decoration-none">
          View all
        </a>
      </div>
      <div className="card-body">
        <table className="table table-sm mb-0">
          <thead>
            <tr>
              <th>Token ID</th>
              <th>User</th>
              <th>Client</th>
              <th className="text-end">Issued At</th>
            </tr>
          </thead>
          <tbody>
            {tokens.map((token) => (
              <tr key={token.id}>
                <td>{token.id}</td>
                <td>{token.user}</td>
                <td>{token.client}</td>
                <td className="text-end">{token.issuedAt}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default RecentTokensTable;
