import React, { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import Pagination from "../common/pagination";
import { usePermissions } from "../../_hooks/usePermissions";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "Sequence",
  sortOrder: "asc",
  searchAll: false,
};

const buildSearchCriterias = (filters) => {
  const criterias = [];
  const nameValue = filters.name.trim() || filters.search.trim();
  if (nameValue.length >= 3) {
    criterias.push({
      ColumnName: "PermissionName",
      Value: nameValue,
      ColumnType: 1,
    });
  }
  const keyValue = filters.key.trim();
  if (keyValue.length >= 3) {
    criterias.push({
      ColumnName: "PermissionKey",
      Value: keyValue,
      ColumnType: 1,
    });
  }
  if (filters.controlType) {
    criterias.push({
      ColumnName: "ControlType",
      Value: filters.controlType,
      ColumnType: 1,
    });
  }
  if (filters.status) {
    criterias.push({
      ColumnName: "Active",
      Value: filters.status,
      ColumnType: 1,
    });
  }
  return criterias;
};

function PermissionsList() {
  const { state, loadPermissions } = usePermissions();
  const navigate = useNavigate();
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const selectAllRef = useRef(null);
  const isFirstPermissionsLoad = useRef(true);
  const [filters, setFilters] = useState({
    name: "",
    key: "",
    controlType: "",
    status: "",
    search: "",
  });

  const totalCount = state.totalCount || 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const showInitialLoading = !state.hasLoadedPermissions;
  const showRefreshingState =
    state.hasLoadedPermissions && state.loadingPermissions;

  useEffect(() => {
    const request = {
      ...defaultSearch,
      pageNumber,
      pageSize,
      SearchCriterias: buildSearchCriterias(filters),
    };

    if (isFirstPermissionsLoad.current) {
      isFirstPermissionsLoad.current = false;
      loadPermissions(request);
      return;
    }

    const timeout = setTimeout(() => {
      loadPermissions(request);
    }, 250);
    return () => clearTimeout(timeout);
  }, [loadPermissions, pageNumber, pageSize, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  const getField = (item, ...keys) =>
    keys.find((key) => item?.[key] !== undefined) !== undefined
      ? item[keys.find((key) => item?.[key] !== undefined)]
      : undefined;

  const getStatusLabel = (item) =>
    getField(item, "active", "Active", "status", "Status") || "Unknown";

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

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Permissions View</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header">Filters</div>
          <div className="filters-grid">
            <div className="filter-field">
              <label className="form-label">Permission Name</label>
              <input
                className="form-control"
                placeholder="Filter by name (min 3 chars)"
                value={filters.name}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    name: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              />
            </div>
            <div className="filter-field">
              <label className="form-label">Permission Key</label>
              <input
                className="form-control"
                placeholder="Filter by key (min 3 chars)"
                value={filters.key}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    key: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              />
            </div>
            <div className="filter-field">
              <label className="form-label">Control Type</label>
              <select
                className="form-select"
                value={filters.controlType}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    controlType: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Types</option>
                <option value="Link">Link</option>
                <option value="Action">Action</option>
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
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
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
            <div className="btn-group">
              <button className="btn btn-soft dropdown-toggle" type="button">
                <i className="fa fa-download"></i> Export
              </button>
            </div>
            <Link className="btn btn-primary" to="new">
              <i className="fa fa-plus"></i> Add New
            </Link>
          </div>
        </div>

        {showInitialLoading ? (
          <div className="text-center py-5">Loading permissions...</div>
        ) : (
          <div className="position-relative">
            {showRefreshingState && (
              <div className="px-3 pt-2 text-muted small">
                Refreshing permissions...
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
                    <th>Key</th>
                    <th>Control Type</th>
                    <th>URL</th>
                    <th>Sequence</th>
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
                            if (id === undefined || id === null) return;
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
                      <td>
                        {getField(item, "permissionName", "PermissionName")}
                      </td>
                      <td className="text-muted">
                        {getField(item, "permissionKey", "PermissionKey")}
                      </td>
                      <td>{getField(item, "controlType", "ControlType")}</td>
                      <td className="text-muted">
                        {getField(item, "url", "Url")}
                      </td>
                      <td>{getField(item, "sequence", "Sequence")}</td>
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
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          title="Edit"
                          onClick={() => {
                            const id = getField(item, "id", "Id");
                            const permissionKey = getField(
                              item,
                              "permissionKey",
                              "PermissionKey",
                            );
                            if (!permissionKey) {
                              return;
                            }
                            navigate(
                              `edit/${encodeURIComponent(String(permissionKey))}`,
                              {
                                state: { id },
                              },
                            );
                          }}
                        >
                          <i className="fa fa-pen"></i>
                        </button>
                      </td>
                    </tr>
                  ))}
                  {state.items.length === 0 && (
                    <tr>
                      <td colSpan="8" className="text-center text-muted py-4">
                        No permissions found.
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
    </div>
  );
}

export default PermissionsList;
