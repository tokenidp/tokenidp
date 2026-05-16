import React, { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../../_hooks/useAuth";
import Breadcrumbs from "../common/breadcrumbs";
import Pagination from "../common/pagination";
import { useRoles } from "../../_hooks/useRoles";
import { useUsers } from "../../_hooks/useUsers";
import { downloadCsv } from "../../_utils/csvExport";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "FullName",
  sortOrder: "desc",
  searchAll: false,
};

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const getItems = (result) => {
  const items = result?.items ?? result?.Items ?? result;
  return Array.isArray(items) ? items : [];
};

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

  if (filters.status) {
    criterias.push({
      ColumnName: "Status",
      Value: filters.status,
      ColumnType: 1,
    });
  }

  return criterias;
};

function RoleUsers() {
  const user = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { roleId: roleIdParam } = useParams();
  const roleId = Number(roleIdParam || 0);
  const { state: rolesState, loadRoleUsers, getRoleById } = useRoles();
  const { state: usersState, loadLookups } = useUsers();
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [filters, setFilters] = useState({
    email: "",
    phone: "",
    status: "",
    search: "",
  });
  const [items, setItems] = useState([]);
  const [totalCount, setTotalCount] = useState(
    Number(location.state?.totalUsers || 0),
  );
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [hasLoadedUsers, setHasLoadedUsers] = useState(false);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [roleName, setRoleName] = useState(
    String(location.state?.roleName || ""),
  );
  const isFirstUsersLoad = useRef(true);
  const selectAllRef = useRef(null);
  const permissionKeys = normalizePermissions(user);
  const canAddUsers = permissionKeys.includes("users.add");

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const showInitialLoading = !hasLoadedUsers;
  const showRefreshingState = hasLoadedUsers && loadingUsers;

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    if (!roleId || roleName) {
      return;
    }

    let isMounted = true;

    const loadRole = async () => {
      const role = await getRoleById(roleId);
      if (!isMounted || !role) {
        return;
      }

      setRoleName(
        String(role.roleName ?? role.RoleName ?? role.name ?? role.Name ?? ""),
      );
    };

    loadRole();

    return () => {
      isMounted = false;
    };
  }, [getRoleById, roleId, roleName]);

  useEffect(() => {
    const hasShortTextFilter = ["email", "phone", "search"].some((key) => {
      const value = filters[key].trim();
      return value.length > 0 && value.length < 3;
    });

    if (hasShortTextFilter || !roleId) {
      return;
    }

    let isMounted = true;

    const request = {
      ...defaultSearch,
      pageNumber,
      pageSize,
      SearchCriterias: buildSearchCriterias(filters),
    };

    const fetchUsers = async () => {
      setLoadingUsers(true);
      const result = await loadRoleUsers(roleId, request);

      if (!isMounted) {
        return;
      }

      const nextItems = getItems(result);
      const nextTotalCount =
        result?.totalCount ??
        result?.TotalCount ??
        (Array.isArray(nextItems) ? nextItems.length : 0);

      setItems(nextItems);
      setTotalCount(Number(nextTotalCount || 0));
      setHasLoadedUsers(true);
      setLoadingUsers(false);
    };

    if (isFirstUsersLoad.current) {
      isFirstUsersLoad.current = false;
      fetchUsers();
      return () => {
        isMounted = false;
      };
    }

    const timeout = setTimeout(fetchUsers, 250);

    return () => {
      isMounted = false;
      clearTimeout(timeout);
    };
  }, [filters, loadRoleUsers, pageNumber, pageSize, roleId]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  const displayedIds = items
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
      items
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
  }, [items]);

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
      const statusLookup = usersState.statuses.find(
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

  const handleExport = () => {
    const rowsToExport = items.filter((item) => {
      const id = getField(item, "id", "Id");
      return selectedIds.size === 0 || selectedIds.has(id);
    });

    const safeRoleName =
      roleName
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "") || `role-${roleId}`;

    downloadCsv(
      `${safeRoleName}-users-${new Date().toISOString().slice(0, 10)}.csv`,
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
          <h5 className="page-title mb-1">Assigned Users</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" appendLabel={roleName} />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header-row">
            <div>
              <div className="filters-header">Role Membership</div>
              <div className="text-muted small">
                {roleName
                  ? `Users currently assigned to ${roleName}.`
                  : "Users currently assigned to this role."}
              </div>
            </div>
            <div className="status-pill status-pill-secondary role-users-summary">
              <i className="fa fa-users" aria-hidden="true"></i>
              <span>{totalCount} assigned</span>
            </div>
          </div>
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
                {usersState.statuses.map((status) => (
                  <option
                    key={status.key ?? status.id ?? status.Id}
                    value={status.value ?? status.name ?? status.Name}
                  >
                    {status.value ?? status.name ?? status.Name}
                  </option>
                ))}
              </select>
            </div>
            <div className="filter-field">
              <label className="form-label">Role</label>
              <input
                className="form-control"
                value={roleName}
                readOnly
                disabled
              />
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
                placeholder="Search assigned users"
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
                disabled={items.length === 0}
                onClick={handleExport}
              >
                <i className="fa fa-download"></i> Export
              </button>
            </div>
            {canAddUsers && (
              <button
                className="btn btn-primary"
                type="button"
                onClick={() =>
                  navigate("/users/new", {
                    state: {
                      preselectedRoleId: roleId,
                      preselectedRoleName: roleName,
                      returnTo: `/roles/users/${roleId}`,
                      returnToState: { roleName },
                    },
                  })
                }
              >
                <i className="fa fa-user-plus"></i> Add User To Role
              </button>
            )}
          </div>
        </div>

        {showInitialLoading ? (
          <div className="text-center py-5">Loading assigned users...</div>
        ) : (
          <div className="position-relative">
            {showRefreshingState && (
              <div className="px-3 pt-2 text-muted small">
                Refreshing assigned users...
              </div>
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
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
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
                    </tr>
                  ))}
                  {items.length === 0 && (
                    <tr>
                      <td colSpan="6" className="text-center text-muted py-4">
                        No assigned users found.
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

        {rolesState.error && <div className="text-danger mt-3">{rolesState.error}</div>}
        {usersState.error && <div className="text-danger mt-3">{usersState.error}</div>}
      </div>
    </div>
  );
}

export default RoleUsers;
