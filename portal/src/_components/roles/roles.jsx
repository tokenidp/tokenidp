import React, { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../_hooks/useAuth";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import { useRoles } from "../../_hooks/useRoles";

const normalizePermissions = (user) => {
  const rawPermissions = user?.permissions ?? user?.Permissions ?? [];
  let permissions = [];

  if (Array.isArray(rawPermissions)) {
    permissions = rawPermissions;
  } else if (typeof rawPermissions === "string") {
    try {
      const parsed = JSON.parse(rawPermissions);
      permissions = Array.isArray(parsed) ? parsed : [];
    } catch {
      permissions = [];
    }
  }

  return permissions
    .map(
      (permission) =>
        permission?.permissionKey || permission?.PermissionKey || permission?.Key,
    )
    .filter(Boolean)
    .map((permissionKey) => String(permissionKey).trim().toLowerCase());
};

const defaultSearch = {
  pageNumber: 1,
  pageSize: 12,
  sortColumn: "RoleName",
  sortOrder: "asc",
  searchAll: true,
};

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const getItems = (result) => {
  const items = result?.items ?? result?.Items ?? result;
  return Array.isArray(items) ? items : [];
};

const buildRoleUserCountMap = (counts) => {
  const map = {};

  getItems(counts).forEach((item) => {
    const roleId = Number(getField(item, "roleId", "RoleId"));
    if (roleId <= 0) {
      return;
    }

    map[roleId] = Number(getField(item, "totalUsers", "TotalUsers") || 0);
  });

  return map;
};

function Roles() {
  const user = useAuth();
  const navigate = useNavigate();
  const { state, loadRoles, loadRoleUserCounts, deleteRole } = useRoles();
  const showInitialLoading = !state.hasLoadedRoles;
  const showRefreshingState = state.hasLoadedRoles && state.loadingRoles;
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteRole, setPendingDeleteRole] = useState(null);
  const [roleUserCounts, setRoleUserCounts] = useState({});
  const [loadingRoleUserCounts, setLoadingRoleUserCounts] = useState(false);
  const permissionKeys = normalizePermissions(user);
  const canDeleteRoles = permissionKeys.includes("roles.delete");
  const canViewAssignedUsers = permissionKeys.includes("users.view");

  const refreshRoles = useCallback(async () => {
    const result = await loadRoles(defaultSearch);
    const items = getItems(result);
    const roleIds = items
      .map((item) => Number(getField(item, "id", "Id")))
      .filter((id) => Number.isInteger(id) && id > 0);

    if (roleIds.length === 0) {
      setRoleUserCounts({});
      setLoadingRoleUserCounts(false);
      return;
    }

    setLoadingRoleUserCounts(true);
    try {
      const counts = await loadRoleUserCounts(roleIds);
      setRoleUserCounts(buildRoleUserCountMap(counts));
    } finally {
      setLoadingRoleUserCounts(false);
    }
  }, [loadRoleUserCounts, loadRoles]);

  useEffect(() => {
    refreshRoles();
  }, [refreshRoles]);

  const requestDelete = (role) => {
    setPendingDeleteRole(role);
    setConfirmOpen(true);
  };

  const closeConfirm = () => {
    setConfirmOpen(false);
    setPendingDeleteRole(null);
  };

  const confirmDelete = async () => {
    if (!pendingDeleteRole?.id) {
      closeConfirm();
      return;
    }

    const isSuccess = await deleteRole(pendingDeleteRole.id);
    closeConfirm();
    if (isSuccess) {
      refreshRoles();
    }
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
            const totalUsers = roleUserCounts[roleId] ?? 0;
            return (
              <div key={roleId} className="col-12 col-md-6 col-xl-4">
                <div className="role-card">
                  <div className="role-card-top">
                    <span className="text-muted">&nbsp;</span>
                    {canViewAssignedUsers ? (
                      <button
                        className="btn btn-link role-user-count-link"
                        type="button"
                        onClick={() =>
                          navigate(`users/${roleId}`, {
                            state: {
                              roleName,
                              totalUsers,
                            },
                          })
                        }
                      >
                        <i className="fa fa-users" aria-hidden="true"></i>
                        <span>Total users</span>
                        <span className="role-user-count-value">
                          {loadingRoleUserCounts ? "..." : totalUsers}
                        </span>
                      </button>
                    ) : (
                      <span className="role-user-count-text text-muted">
                        <i className="fa fa-users" aria-hidden="true"></i>
                        <span>
                          Total users {loadingRoleUserCounts ? "..." : totalUsers}
                        </span>
                      </span>
                    )}
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
                      {canDeleteRoles && (
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
                      )}
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
            ? `Delete ${pendingDeleteRole.name}?`
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
