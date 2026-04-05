import React, { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import Pagination from "../common/pagination";
import InfoModal from "../common/infoModal";
import { useUsers } from "../../_hooks/useUsers";
import { downloadCsv } from "../../_utils/csvExport";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "FullName",
  sortOrder: "desc",
  searchAll: false,
};

const buildSearchCriterias = (filters) => {
  const criterias = [];
  if (filters.search.trim()) {
    criterias.push({
      ColumnName: "Search",
      Value: filters.search.trim(),
      ColumnType: 1,
    });
  }
  if (filters.email.trim()) {
    criterias.push({
      ColumnName: "Email",
      Value: filters.email.trim(),
      ColumnType: 1,
    });
  }
  if (filters.phone.trim()) {
    criterias.push({
      ColumnName: "PhoneNumber",
      Value: filters.phone.trim(),
      ColumnType: 1,
    });
  }
  if (filters.role) {
    criterias.push({
      ColumnName: "Roles",
      Value: filters.role,
      ColumnType: 1,
    });
  }
  if (filters.status) {
    criterias.push({
      ColumnName: "Status",
      Value: filters.status,
      ColumnType: 1,
    });
  }
  return criterias;
};

function Users() {
  const navigate = useNavigate();
  const { state, loadUsers, loadLookups, resetUserPassword, updateUserStatus } =
    useUsers();
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [filters, setFilters] = useState({
    email: "",
    phone: "",
    role: "",
    status: "",
    search: "",
  });
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [resetPasswordSubmitting, setResetPasswordSubmitting] = useState(false);
  const [resetConfirmOpen, setResetConfirmOpen] = useState(false);
  const [pendingResetUser, setPendingResetUser] = useState({
    id: 0,
    userName: "",
  });
  const [statusUpdatingUserId, setStatusUpdatingUserId] = useState(0);
  const [statusConfirmOpen, setStatusConfirmOpen] = useState(false);
  const [pendingStatusUpdate, setPendingStatusUpdate] = useState({
    id: 0,
    userName: "",
    status: "",
  });
  const [infoOpen, setInfoOpen] = useState(false);
  const [infoContent, setInfoContent] = useState({ title: "", message: "" });
  const isFirstUsersLoad = useRef(true);
  const selectAllRef = useRef(null);

  const totalCount = state.totalCount || 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const showInitialLoading = !state.hasLoadedUsers;
  const showRefreshingState = state.hasLoadedUsers && state.loadingUsers;

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    const hasShortTextFilter = ["email", "phone", "search"].some((key) => {
      const value = filters[key].trim();
      return value.length > 0 && value.length < 3;
    });

    if (hasShortTextFilter) {
      return;
    }

    const request = {
      ...defaultSearch,
      pageNumber,
      pageSize,
      SearchCriterias: buildSearchCriterias(filters),
    };

    if (isFirstUsersLoad.current) {
      isFirstUsersLoad.current = false;
      loadUsers(request);
      return;
    }

    const timeout = setTimeout(() => {
      loadUsers(request);
    }, 250);

    return () => clearTimeout(timeout);
  }, [loadUsers, pageNumber, pageSize, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  const getField = (item, ...keys) =>
    keys.find((key) => item?.[key] !== undefined) !== undefined
      ? item[keys.find((key) => item?.[key] !== undefined)]
      : undefined;

  const displayedIds = state.items
    .map((item) => getField(item, "id", "Id"))
    .filter((id) => id !== undefined && id !== null);
  const allSelected =
    displayedIds.length > 0 && displayedIds.every((id) => selectedIds.has(id));
  const someSelected =
    displayedIds.some((id) => selectedIds.has(id)) && !allSelected;

  useEffect(() => {
    if (selectAllRef.current) {
      selectAllRef.current.indeterminate = someSelected;
    }
  }, [someSelected]);

  useEffect(() => {
    const visibleIds = new Set(
      state.items
        .map((item) => getField(item, "id", "Id"))
        .filter((id) => id !== undefined && id !== null),
    );
    setSelectedIds((prev) => {
      let hasChanges = false;
      const next = new Set();
      prev.forEach((id) => {
        if (visibleIds.has(id)) {
          next.add(id);
        } else {
          hasChanges = true;
        }
      });
      return hasChanges ? next : prev;
    });
  }, [state.items]);

  const formatRoles = (value) => {
    if (Array.isArray(value)) {
      return value
        .map((role) => {
          if (typeof role === "string") {
            return role;
          }

          return role?.name ?? role?.Name ?? role?.value ?? role?.Value ?? "";
        })
        .filter(Boolean)
        .join(", ");
    }

    return String(value ?? "");
  };

  const getStatusLabel = (item) => {
    const isActive = getField(item, "isActive", "IsActive");
    if (typeof isActive === "boolean") {
      return isActive ? "Active" : "Disabled";
    }

    const statusId = getField(item, "statusId", "StatusId");
    if (statusId !== undefined && statusId !== null) {
      const statusLookup = state.statuses.find(
        (status) =>
          String(status.key ?? status.id ?? status.Id) === String(statusId),
      );
      const statusValue =
        statusLookup?.value ?? statusLookup?.name ?? statusLookup?.Name;
      if (statusValue) {
        return String(statusValue);
      }
    }

    return getField(item, "status", "Status") || "Unknown";
  };

  const getNextStatus = (item) => {
    const currentStatus = String(getStatusLabel(item)).toLowerCase();
    return currentStatus === "active" ? "Inactive" : "Active";
  };

  const openResetPassword = (item) => {
    const id = Number(getField(item, "id", "Id") || 0);
    const userName = String(
      getField(item, "userName", "UserName", "email", "Email") || "",
    );
    if (!id) {
      return;
    }
    setPendingResetUser({ id, userName });
    setResetConfirmOpen(true);
  };

  const closeResetPassword = () => {
    if (resetPasswordSubmitting) {
      return;
    }
    setResetConfirmOpen(false);
    setPendingResetUser({ id: 0, userName: "" });
  };

  const submitResetPassword = async () => {
    if (!pendingResetUser.id || resetPasswordSubmitting) {
      return false;
    }

    setResetPasswordSubmitting(true);
    const response = await resetUserPassword(pendingResetUser.id);
    setResetPasswordSubmitting(false);

    closeResetPassword();

    if (!response) {
      return false;
    }

    setInfoContent({
      title: "Reset email queued",
      message: "Password reset link has been queued for delivery.",
    });
    setInfoOpen(true);
    return true;
  };

  const openStatusConfirm = (item) => {
    const id = Number(getField(item, "id", "Id") || 0);
    if (!id || statusUpdatingUserId) {
      return;
    }

    setPendingStatusUpdate({
      id,
      userName: String(
        getField(item, "fullName", "name", "Name", "userName", "UserName") ||
          "this user",
      ),
      status: getNextStatus(item),
    });
    setStatusConfirmOpen(true);
  };

  const closeStatusConfirm = () => {
    if (statusUpdatingUserId) {
      return;
    }

    setStatusConfirmOpen(false);
    setPendingStatusUpdate({ id: 0, userName: "", status: "" });
  };

  const submitUserStatusUpdate = async () => {
    const { id, status: nextStatus } = pendingStatusUpdate;
    if (!id || !nextStatus) {
      return;
    }

    setStatusUpdatingUserId(id);
    const isSuccess = await updateUserStatus(id, { id, status: nextStatus });
    setStatusUpdatingUserId(0);

    closeStatusConfirm();

    if (!isSuccess) {
      return;
    }

    await loadUsers({
      ...defaultSearch,
      pageNumber,
      pageSize,
      SearchCriterias: buildSearchCriterias(filters),
    });

    setInfoContent({
      title: "Status updated",
      message: `User status changed to ${nextStatus}.`,
    });
    setInfoOpen(true);
  };

  const handleExport = () => {
    const rowsToExport = state.items.filter((item) => {
      const id = getField(item, "id", "Id");
      return selectedIds.size === 0 || selectedIds.has(id);
    });

    downloadCsv(
      `users-${new Date().toISOString().slice(0, 10)}.csv`,
      [
        {
          header: "Name",
          accessor: (item) => getField(item, "fullName", "name", "Name"),
        },
        {
          header: "Email",
          accessor: (item) =>
            getField(item, "email", "Email", "userName", "UserName"),
        },
        {
          header: "Phone Number",
          accessor: (item) => getField(item, "phoneNumber", "PhoneNumber"),
        },
        {
          header: "Roles",
          accessor: (item) => formatRoles(getField(item, "roles", "Roles")),
        },
        {
          header: "Status",
          accessor: (item) => getStatusLabel(item),
        },
      ],
      rowsToExport,
    );
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Users View</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header">Filters</div>
          <div className="filters-grid">
            <div className="filter-field">
              <label className="form-label">Email</label>
              <input
                className="form-control"
                placeholder="Filter by Email"
                value={filters.email}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    email: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              />
            </div>
            <div className="filter-field">
              <label className="form-label">Phone Number</label>
              <input
                className="form-control"
                placeholder="Filter by Phone"
                value={filters.phone}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    phone: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              />
            </div>
            <div className="filter-field">
              <label className="form-label">Roles</label>
              <select
                className="form-select"
                value={filters.role}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    role: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Roles</option>
                {state.roles.map((role) => (
                  <option
                    key={role.key ?? role.id ?? role.Id}
                    value={role.value ?? role.name ?? role.Name}
                  >
                    {role.value ?? role.name ?? role.Name}
                  </option>
                ))}
              </select>
            </div>
            <div className="filter-field">
              <label className="form-label">Status</label>
              <select
                className="form-select"
                value={filters.status}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    status: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Status</option>
                {state.statuses.map((status) => (
                  <option
                    key={status.key ?? status.id ?? status.Id}
                    value={status.value ?? status.name ?? status.Name}
                  >
                    {status.value ?? status.name ?? status.Name}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <div className="table-toolbar">
          <div className="table-length">
            <select
              className="form-select"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPageNumber(1);
              }}
            >
              <option>10</option>
              <option>25</option>
              <option>50</option>
            </select>
          </div>
          <div className="table-toolbar-actions">
            <div className="table-search">
              <i className="fa fa-search"></i>
              <input
                type="text"
                className="form-control"
                placeholder="Search users"
                value={filters.search}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    search: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              />
            </div>
            <div className="btn-group">
              <button
                className="btn btn-soft dropdown-toggle"
                type="button"
                disabled={state.items.length === 0}
                onClick={handleExport}
              >
                <i className="fa fa-download"></i> Export
              </button>
            </div>
            <Link className="btn btn-primary" to="new">
              <i className="fa fa-plus"></i> Add New
            </Link>
          </div>
        </div>

        {showInitialLoading ? (
          <div className="text-center py-5">Loading users...</div>
        ) : (
          <div className="position-relative">
            {showRefreshingState && (
              <div className="px-3 pt-2 text-muted small">Refreshing users...</div>
            )}
            <div className="table-responsive">
              <table className="table table-hover align-middle table-striped table-bordered">
                <thead>
                  <tr>
                    <th className="table-checkbox">
                      <input
                        ref={selectAllRef}
                        type="checkbox"
                        checked={allSelected}
                        disabled={displayedIds.length === 0}
                        onChange={(event) => {
                          const isChecked = event.target.checked;
                          setSelectedIds((prev) => {
                            const next = new Set(prev);
                            displayedIds.forEach((id) => {
                              if (isChecked) {
                                next.add(id);
                              } else {
                                next.delete(id);
                              }
                            });
                            return next;
                          });
                        }}
                      />
                    </th>
                    <th>Name</th>
                    <th className="col-email">Email</th>
                    <th className="col-phone">Phone Number</th>
                    <th className="col-roles">Roles</th>
                    <th>Status</th>
                    <th className="text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {state.items.map((item) => (
                    <tr key={getField(item, "id", "Id")}>
                      <td className="table-checkbox">
                        <input
                          type="checkbox"
                          checked={selectedIds.has(getField(item, "id", "Id"))}
                          onChange={(event) => {
                            const id = getField(item, "id", "Id");
                            if (id === undefined || id === null) {
                              return;
                            }

                            const isChecked = event.target.checked;
                            setSelectedIds((prev) => {
                              const next = new Set(prev);
                              if (isChecked) {
                                next.add(id);
                              } else {
                                next.delete(id);
                              }
                              return next;
                            });
                          }}
                        />
                      </td>
                      <td>{getField(item, "fullName", "name", "Name")}</td>
                      <td className="text-muted col-email">
                        {getField(item, "email", "Email", "userName", "UserName")}
                      </td>
                      <td className="col-phone">
                        {getField(item, "phoneNumber", "PhoneNumber")}
                      </td>
                      <td className="col-roles">
                        {formatRoles(getField(item, "roles", "Roles"))}
                      </td>
                      <td>
                        <span
                          className={`status-pill ${
                            getStatusLabel(item) === "Active"
                              ? "status-pill-success"
                              : "status-pill-off"
                          }`}
                        >
                          {getStatusLabel(item)}
                        </span>
                      </td>
                      <td className="text-right table-actions">
                        <button
                          className="btn btn-link p-0 text-warning ButtonLink"
                          type="button"
                          onClick={() => openResetPassword(item)}
                          title="Reset Password"
                        >
                          <i className="fa fa-key"></i>
                        </button>
                        <button
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          onClick={() => {
                            const id = getField(item, "id", "Id");
                            const userName = getField(
                              item,
                              "userName",
                              "UserName",
                            );
                            if (!userName) {
                              return;
                            }
                            navigate(
                              `edit/${encodeURIComponent(String(userName))}`,
                              {
                                state: { id },
                              },
                            );
                          }}
                          title="Edit"
                        >
                          <i className="fa fa-pen"></i>
                        </button>
                        <button
                          className={`btn btn-link p-0 ButtonLink ${
                            getNextStatus(item) === "Inactive"
                              ? "text-danger"
                              : "text-success"
                          }`}
                          type="button"
                          title={`Set ${getNextStatus(item)}`}
                          disabled={
                            statusUpdatingUserId ===
                            Number(getField(item, "id", "Id"))
                          }
                          onClick={() => openStatusConfirm(item)}
                        >
                          <i
                            className={`fa ${
                              getNextStatus(item) === "Inactive"
                                ? "fa-ban"
                                : "fa-check"
                            }`}
                          ></i>
                        </button>
                      </td>
                    </tr>
                  ))}
                  {state.items.length === 0 && (
                    <tr>
                      <td colSpan="7" className="text-center text-muted py-4">
                        No users found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {!showInitialLoading && (
          <Pagination
            pageNumber={pageNumber}
            pageSize={pageSize}
            totalCount={totalCount}
            onPageChange={setPageNumber}
          />
        )}
      </div>

      <InfoModal
        open={infoOpen}
        title={infoContent.title}
        message={infoContent.message}
        onClose={() => setInfoOpen(false)}
      />

      <ConfirmModal
        open={resetConfirmOpen}
        title="Initiate Password Reset"
        message={
          pendingResetUser.id
            ? `Send a password reset email to ${pendingResetUser.userName || "this user"}?`
            : "Send password reset email?"
        }
        confirmLabel={
          resetPasswordSubmitting ? "Sending..." : "Send Reset Email"
        }
        onConfirm={submitResetPassword}
        onClose={closeResetPassword}
      />

      <ConfirmModal
        open={statusConfirmOpen}
        title="Update User Status"
        message={
          pendingStatusUpdate.id
            ? `Change status for ${pendingStatusUpdate.userName} to ${pendingStatusUpdate.status}?`
            : "Change user status?"
        }
        confirmLabel={pendingStatusUpdate.status || "Confirm"}
        onConfirm={submitUserStatusUpdate}
        onClose={closeStatusConfirm}
      />
    </div>
  );
}

export default Users;
