import React, { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import { useApiResources } from "../../_hooks/useApiResources";

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

function ApiResourcesList() {
  const { state, loadApiResources, deleteApiResource } = useApiResources();
  const navigate = useNavigate();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);

  useEffect(() => {
    loadApiResources();
  }, [loadApiResources]);

  const requestDelete = (id) => {
    setPendingDeleteId(id);
    setConfirmOpen(true);
  };

  const closeConfirm = () => {
    setPendingDeleteId(null);
    setConfirmOpen(false);
  };

  const confirmDelete = async () => {
    if (!pendingDeleteId) {
      closeConfirm();
      return;
    }

    const result = await deleteApiResource(pendingDeleteId);
    closeConfirm();
    if (result.ok) {
      loadApiResources();
    }
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Api Resources</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="table-toolbar">
          <div></div>
          <div className="table-toolbar-actions">
            <Link className="btn btn-primary" to="/api-resources/new">
              <i className="fa fa-plus"></i> Add New
            </Link>
          </div>
        </div>

        {state.loading ? (
          <div className="text-center py-5">Loading ApiResources...</div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover align-middle table-striped table-bordered">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Display Name</th>
                  <th>Scopes</th>
                  <th>Status</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {state.items.map((item) => {
                  const scopes = getField(item, "scopes", "Scopes") || [];
                  return (
                    <tr key={getField(item, "id", "Id")}>
                      <td>{getField(item, "name", "Name")}</td>
                      <td>{getField(item, "displayName", "DisplayName")}</td>
                      <td>
                        {Array.isArray(scopes) && scopes.length
                          ? scopes.map((scope) => scope.name ?? scope.Name).join(", ")
                          : "--"}
                      </td>
                      <td>
                        <span
                          className={`status-pill ${
                            getField(item, "enabled", "Enabled")
                              ? "status-pill-success"
                              : "status-pill-off"
                          }`}
                        >
                          {getField(item, "enabled", "Enabled") ? "Active" : "Disabled"}
                        </span>
                      </td>
                      <td className="text-right table-actions">
                        <button
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          title="Edit"
                          onClick={() =>
                            navigate(`edit/${encodeURIComponent(String(getField(item, "id", "Id")))}`)
                          }
                        >
                          <i className="fa fa-pen"></i>
                        </button>
                        <button
                          className="btn btn-link p-0 text-danger ButtonLink"
                          type="button"
                          title="Delete"
                          onClick={() => requestDelete(getField(item, "id", "Id"))}
                        >
                          <i className="fa fa-trash"></i>
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {state.items.length === 0 && (
                  <tr>
                    <td colSpan="5" className="text-center text-muted py-4">
                      No ApiResources found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <ConfirmModal
        open={confirmOpen}
        title="Delete ApiResource"
        message="Are you sure you want to delete this ApiResource? This action cannot be undone."
        confirmLabel="Delete"
        onConfirm={confirmDelete}
        onClose={closeConfirm}
      />
    </div>
  );
}

export default ApiResourcesList;
