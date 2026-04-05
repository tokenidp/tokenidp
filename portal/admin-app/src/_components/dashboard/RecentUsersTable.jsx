import React from "react";

const users = [
  { name: "Jane Smith", email: "jsmith@example.com", status: "Active", tone: "text-success" },
  { name: "John Doe", email: "john@example.com", status: "Inactive", tone: "text-muted" },
  { name: "Lee Amith", email: "lee@example.com", status: "Active", tone: "text-success" },
  { name: "Teol User", email: "admin@example.com", status: "Admin", tone: "text-info" },
];

function RecentUsersTable() {
  return (
    <div className="card-lite">
      <div className="card-header d-flex justify-content-between">
        <strong>Recent Users</strong>
        <a href="#!" className="small text-decoration-none">
          View all
        </a>
      </div>
      <div className="card-body">
        <table className="table table-sm mb-0">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th className="text-end">Status</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.email}>
                <td>{user.name}</td>
                <td>{user.email}</td>
                <td className={`text-end ${user.tone}`}>{user.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default RecentUsersTable;
