import React, { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import { useRoles } from "../../_hooks/useRoles";

function Roles() {
  const navigate = useNavigate();
  const { state, loadRoles } = useRoles();
  const showInitialLoading = !state.hasLoadedRoles;
  const showRefreshingState = state.hasLoadedRoles && state.loadingRoles;
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteRole, setPendingDeleteRole] = useState(null);

  const defaultSearch = {
    pageNumber: 1,
    pageSize: 12,
    sortColumn: "RoleName",
    sortOrder: "asc",
    searchAll: true,
  };

  useEffect(() => {
    loadRoles(defaultSearch);
  }, [loadRoles]);

  const getField = (item, ...keys) =>
    keys.find((key) => item?.[key] !== undefined) !== undefined
      ? item[keys.find((key) => item?.[key] !== undefined)]
      : undefined;

  const requestDelete = (role) => {
    setPendingDeleteRole(role);
    setConfirmOpen(true);
  };

  const closeConfirm = () => {
    setConfirmOpen(false);
    setPendingDeleteRole(null);
  };

  const confirmDelete = () => {
    closeConfirm();
    alert("Delete role (placeholder).");
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Roles View</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="row g-3 role-grid">
        {showInitialLoading && (
          <div className="col-12">
            <div className="text-center text-muted py-4">Loading roles...</div>
          </div>
        )}
        {showRefreshingState && (
          <div className="col-12">
            <div className="text-muted small">Refreshing roles...</div>
          </div>
        )}
        {!showInitialLoading &&
          state.items.map((role) => {
            const roleId = getField(role, "id", "Id");
            const roleName = getField(
              role,
              "roleName",
              "RoleName",
              "name",
              "Name",
            );
            return (
              <div key={roleId} className="col-12 col-md-6 col-xl-4">
                <div className="role-card">
                  <div className="role-card-top">
                    <span className="text-muted">&nbsp;</span>
                    <span className="text-muted">Total users 5</span>
                  </div>
                  <div className="role-card-body">
                    <h6 className="mb-2">{roleName}</h6>
                    <div className="d-flex align-items-center gap-3">
                      <button
                        className="btn btn-link p-0 text-primary ButtonLink"
                        type="button"
                        onClick={() => {
                          if (!roleName) {
                            return;
                          }
                          navigate(
                            `edit/${encodeURIComponent(String(roleName))}`,
                            {
                              state: { id: roleId },
                            },
                          );
                        }}
                      >
                        <i className="fa fa-edit me-1" aria-hidden="true"></i>
                        Edit Role
                      </button>
                      <button
                        className="btn btn-link p-0 text-danger ButtonLink"
                        type="button"
                        onClick={() =>
                          requestDelete({ id: roleId, name: roleName })
                        }
                      >
                        <i className="fa fa-trash me-1" aria-hidden="true"></i>
                        Delete
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        {!showInitialLoading && state.items.length === 0 && (
          <div className="col-12">
            <div className="text-center text-muted py-4">No roles found.</div>
          </div>
        )}

        <div className="col-12 col-md-6 col-xl-4">
          <div className="role-card role-card-add">
            <div className="role-card-add-body">
              <h6>Add New Role</h6>
              <p className="text-muted mb-3">
                Add new role, if it doesn&#39;t exist.
              </p>
              <Link className="btn btn-primary no-hover" to="new">
                Add New
              </Link>
            </div>
          </div>
        </div>
      </div>

      <ConfirmModal
        open={confirmOpen}
        title="Delete Role"
        message={
          pendingDeleteRole
            ? `Delete ${pendingDeleteRole.name}? This action cannot be undone.`
            : "Delete this role?"
        }
        confirmLabel="Delete"
        onConfirm={confirmDelete}
        onClose={closeConfirm}
      />
    </div>
  );
}

export default Roles;
