import React, { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import Pagination from "../common/pagination";
import { useTenants } from "../../_hooks/useTenants";
import { downloadCsv } from "../../_utils/csvExport";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "TenantName",
  sortOrder: "asc",
  searchAll: false,
};

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const getLookupLabel = (options, value) => {
  const normalized = String(value ?? "");
  const match = (options || []).find(
    (option) =>
      String(option?.key ?? option?.id ?? option?.Key ?? option?.Id) ===
      normalized,
  );
  return (
    match?.value ?? match?.name ?? match?.Value ?? match?.Name ?? "Unknown"
  );
};

function TenantsList() {
  const navigate = useNavigate();
  const { state, loadTenants, loadLookups, deleteTenant } = useTenants();
  const isFirstTenantsLoad = React.useRef(true);
  const selectAllRef = React.useRef(null);
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [filters, setFilters] = useState({
    status: "",
    search: "",
  });
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);

  const totalCount = state.totalCount || 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const showInitialLoading = !state.hasLoadedTenants;
  const showRefreshingState = state.hasLoadedTenants && state.loadingTenants;

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  const buildSearchCriterias = () => {
    const criterias = [];
    if (filters.search.trim()) {
      criterias.push({
        ColumnName: "Search",
        Value: filters.search.trim(),
        ColumnType: 1,
      });
    }
    if (filters.status) {
      criterias.push({
        ColumnName: "IsActive",
        Value: filters.status,
        ColumnType: 1,
      });
    }
    return criterias;
  };

  useEffect(() => {
    const hasShortSearch =
      filters.search.trim().length > 0 && filters.search.trim().length < 3;

    if (hasShortSearch) {
      return () => {};
    }

    const request = {
      ...defaultSearch,
      pageNumber,
      pageSize,
      SearchCriterias: buildSearchCriterias(),
    };

    if (isFirstTenantsLoad.current) {
      isFirstTenantsLoad.current = false;
      loadTenants(request);
      return () => {};
    }

    const timeout = setTimeout(() => {
      loadTenants(request);
    }, 250);

    return () => clearTimeout(timeout);
  }, [loadTenants, pageNumber, pageSize, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

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

  const requestDelete = (id) => {
    setPendingDeleteId(id);
    setConfirmOpen(true);
  };

  const closeConfirm = () => {
    setConfirmOpen(false);
    setPendingDeleteId(null);
  };

  const confirmDelete = async () => {
    if (!pendingDeleteId) {
      closeConfirm();
      return;
    }

    const result = await deleteTenant(pendingDeleteId);
    closeConfirm();

    if (result.ok) {
      loadTenants({
        ...defaultSearch,
        pageNumber,
        pageSize,
        SearchCriterias: buildSearchCriterias(),
      });
    }
  };

  const resolveStatus = (item) => {
    const isActive = getField(item, "isActive", "IsActive");
    const key = String(isActive).toLowerCase();
    return (
      getLookupLabel(state.statuses, key) ||
      (key === "true" ? "Active" : "Inactive")
    );
  };

  const handleExport = () => {
    const rowsToExport = state.items.filter((item) => {
      const id = getField(item, "id", "Id");
      return selectedIds.size === 0 || selectedIds.has(id);
    });

    downloadCsv(
      `tenants-${new Date().toISOString().slice(0, 10)}.csv`,
      [
        {
          header: "Tenant Name",
          accessor: (item) => getField(item, "tenantName", "TenantName"),
        },
        {
          header: "Tenant Code",
          accessor: (item) => getField(item, "tenantCode", "TenantCode"),
        },
        {
          header: "Tenant Key",
          accessor: (item) => getField(item, "tenantKey", "TenantKey"),
        },
        {
          header: "Email",
          accessor: (item) => getField(item, "email", "Email"),
        },
        {
          header: "Authentication Mode",
          accessor: (item) =>
            getLookupLabel(
              state.authenticationModes,
              getField(item, "authenticationMode", "AuthenticationMode"),
            ),
        },
        {
          header: "Status",
          accessor: (item) => resolveStatus(item),
        },
      ],
      rowsToExport,
    );
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Tenants List</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header">Filters</div>
          <div className="filters-grid">
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
                {state.statuses.map((option) => (
                  <option
                    key={option.key ?? option.id ?? option.Key ?? option.Id}
                    value={option.key ?? option.id ?? option.Key ?? option.Id}
                  >
                    {option.value ?? option.name ?? option.Value ?? option.Name}
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
                placeholder="Search tenants (min 3 chars)"
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
          <div className="text-center py-5">Loading tenants...</div>
        ) : (
          <div className="position-relative">
            {showRefreshingState && (
              <div className="px-3 pt-2 text-muted small">
                Refreshing tenants...
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
                  <th>Tenant Name</th>
                  <th>Tenant Code</th>
                  <th>Tenant Key</th>
                  <th>Email</th>
                  <th>Authentication Mode</th>
                  <th>Status</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {state.items.map((item) => {
                  const statusLabel = resolveStatus(item);
                  const isActive =
                    String(
                      getField(item, "isActive", "IsActive"),
                    ).toLowerCase() === "true";
                  return (
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
                      <td>{getField(item, "tenantName", "TenantName")}</td>
                      <td className="text-muted">
                        {getField(item, "tenantCode", "TenantCode")}
                      </td>
                      <td className="text-muted">
                        {getField(item, "tenantKey", "TenantKey")}
                      </td>
                      <td>{getField(item, "email", "Email")}</td>
                      <td>
                        {getLookupLabel(
                          state.authenticationModes,
                          getField(
                            item,
                            "authenticationMode",
                            "AuthenticationMode",
                          ),
                        )}
                      </td>
                      <td>
                        <span
                          className={`status-pill ${
                            isActive ? "status-pill-success" : "status-pill-off"
                          }`}
                        >
                          {statusLabel}
                        </span>
                      </td>
                      <td className="text-right table-actions">
                        <button
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          onClick={() => {
                            const id = getField(item, "id", "Id");
                            const tenantCode = getField(
                              item,
                              "tenantCode",
                              "TenantCode",
                            );
                            if (!tenantCode) {
                              return;
                            }
                            navigate(
                              `edit/${encodeURIComponent(String(tenantCode))}`,
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
                          className="btn btn-link p-0 text-danger ButtonLink"
                          type="button"
                          onClick={() =>
                            requestDelete(getField(item, "id", "Id"))
                          }
                          title={
                            isActive ? "Deactivate before deleting" : "Delete"
                          }
                          disabled={isActive}
                        >
                          <i className="fa fa-trash"></i>
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {state.items.length === 0 && (
                  <tr>
                    <td colSpan="8" className="text-center text-muted py-4">
                      No tenants found.
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

      <ConfirmModal
        open={confirmOpen}
        title="Delete Tenant"
        message="Are you sure you want to delete this tenant? This action cannot be undone."
        confirmLabel="Delete"
        onConfirm={confirmDelete}
        onClose={closeConfirm}
      />
    </div>
  );
}

export default TenantsList;
