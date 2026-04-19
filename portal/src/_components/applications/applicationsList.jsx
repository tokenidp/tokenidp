import React, { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "tokenidp-react";
import { useApplications } from "../../_hooks/useApplications";
import defaultApplicationImage from "../../_assets/images/TokenIDP.svg";
import { downloadCsv } from "../../_utils/csvExport";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import Pagination from "../common/pagination";
import { normalizeGrantTypeOptions } from "./wizard/wizardState";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "ClientName",
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

const getLookupValue = (options, key) => {
  const normalized = String(key ?? "");
  const match = (options || []).find(
    (option) =>
      String(option?.key ?? option?.id ?? option?.Key ?? option?.Id) ===
      normalized,
  );
  return match?.value ?? match?.name ?? match?.Value ?? match?.Name ?? "";
};

const getMultiLookupLabels = (options, values) => {
  if (!Array.isArray(values) || values.length === 0) {
    return "";
  }

  const normalizedOptions = (options || []).map((option) => ({
    id: String(option?.id ?? option?.Id ?? ""),
    key: String(option?.key ?? option?.Key ?? "").toLowerCase(),
    value: option?.value ?? option?.name ?? option?.Value ?? option?.Name ?? "",
  }));

  const labels = values
    .map((value) => {
      const raw = String(value ?? "").trim();
      if (!raw) {
        return "";
      }

      const normalizedRaw = raw.toLowerCase();
      const match = normalizedOptions.find(
        (option) =>
          option.id === raw ||
          option.key === normalizedRaw,
      );

      return match?.value ?? raw;
    })
    .filter(Boolean);

  return Array.from(new Set(labels)).join(", ");
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

function ApplicationsList() {
  const user = useAuth();
  const { state, loadApplications, loadLookups, deleteApplication } =
    useApplications();
  const navigate = useNavigate();
  const isFirstApplicationsLoad = useRef(true);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const selectAllRef = useRef(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [viewMode, setViewMode] = useState("card");
  const [filters, setFilters] = useState({
    appType: "",
    tokenType: "",
    status: "",
    search: "",
  });
  const permissionKeys = normalizePermissions(user);
  const canDeleteApplications = permissionKeys.includes("applications.delete");

  const totalCount = state.totalCount || 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const showInitialLoading = !state.hasLoadedApplications;
  const showRefreshingState =
    state.hasLoadedApplications && state.loadingApplications;

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
    if (filters.appType) {
      const value = getLookupValue(state.appTypes, filters.appType);
      if (value) {
        criterias.push({
          ColumnName: "AppType",
          Value: value,
          ColumnType: 1,
        });
      }
    }
    if (filters.tokenType) {
      const value = getLookupValue(state.tokenTypes, filters.tokenType);
      if (value) {
        criterias.push({
          ColumnName: "TokenType",
          Value: value,
          ColumnType: 1,
        });
      }
    }
    if (filters.status) {
      criterias.push({
        ColumnName: "IsActive",
        Value: String(filters.status === "true"),
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

    if (isFirstApplicationsLoad.current) {
      isFirstApplicationsLoad.current = false;
      loadApplications(request);
      return () => {};
    }

    const timeout = setTimeout(() => {
      loadApplications(request);
    }, 250);

    return () => clearTimeout(timeout);
  }, [loadApplications, pageNumber, pageSize, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  const getItemId = (item) => getField(item, "id", "Id");
  const getClientName = (item) => getField(item, "clientName", "ClientName");
  const getClientId = (item) => getField(item, "clientId", "ClientId");
  const getAppType = (item) => getField(item, "appType", "AppType");
  const getTokenType = (item) =>
    getField(
      item,
      "tokenType",
      "TokenType",
      "accessTokenType",
      "AccessTokenType",
    );
  const getGrantTypes = (item) => getField(item, "grantTypes", "GrantTypes");
  const isApplicationActive = (item) => getField(item, "isActive", "IsActive");
  const grantTypeOptions = normalizeGrantTypeOptions(state.grantTypes);
  const getGrantTypeLabel = (item) =>
    getMultiLookupLabels(grantTypeOptions, getGrantTypes(item)) || "-";

  const displayedIds = state.items
    .map((item) => getItemId(item))
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
        .map((item) => getItemId(item))
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
    if (!canDeleteApplications) {
      return;
    }

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

    const result = await deleteApplication(pendingDeleteId);
    closeConfirm();

    if (result.ok) {
      loadApplications({
        ...defaultSearch,
        pageNumber,
        pageSize,
        SearchCriterias: buildSearchCriterias(),
      });
    }
  };

  const handleExport = () => {
    const rowsToExport = state.items.filter((item) => {
      const id = getItemId(item);
      return selectedIds.size === 0 || selectedIds.has(id);
    });

    downloadCsv(
      `applications-${new Date().toISOString().slice(0, 10)}.csv`,
      [
        {
          header: "Client Name",
          accessor: (item) => getClientName(item),
        },
        {
          header: "Client ID",
          accessor: (item) => getClientId(item),
        },
        {
          header: "App Type",
          accessor: (item) => getLookupLabel(state.appTypes, getAppType(item)),
        },
        {
          header: "Token Type",
          accessor: (item) =>
            getLookupLabel(state.tokenTypes, getTokenType(item)),
        },
        {
          header: "Grant Type",
          accessor: (item) => getGrantTypeLabel(item),
        },
        {
          header: "Status",
          accessor: (item) =>
            isApplicationActive(item) ? "Active" : "Disabled",
        },
      ],
      rowsToExport,
    );
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Applications View</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>
      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header-row">
            <div className="filters-header">Filters</div>
            <div
              className="view-toggle"
              role="group"
              aria-label="Application view"
            >
              <button
                type="button"
                className={`view-toggle-button ${
                  viewMode === "card" ? "is-active" : ""
                }`}
                onClick={() => setViewMode("card")}
              >
                Card
              </button>
              <button
                type="button"
                className={`view-toggle-button ${
                  viewMode === "table" ? "is-active" : ""
                }`}
                onClick={() => setViewMode("table")}
              >
                Table
              </button>
            </div>
          </div>
          <div className="filters-grid">
            <div className="filter-field">
              <label className="form-label">App Type</label>
              <select
                className="form-select"
                value={filters.appType}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    appType: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Apps</option>
                {state.appTypes.map((option) => (
                  <option
                    key={option.key ?? option.id ?? option.Key ?? option.Id}
                    value={option.key ?? option.id ?? option.Key ?? option.Id}
                  >
                    {option.value ?? option.name ?? option.Value ?? option.Name}
                  </option>
                ))}
              </select>
            </div>
            <div className="filter-field">
              <label className="form-label">Token Type</label>
              <select
                className="form-select"
                value={filters.tokenType}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    tokenType: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Tokens</option>
                {state.tokenTypes.map((option) => (
                  <option
                    key={option.key ?? option.id ?? option.Key ?? option.Id}
                    value={option.key ?? option.id ?? option.Key ?? option.Id}
                  >
                    {option.value ?? option.name ?? option.Value ?? option.Name}
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
                <option value="true">Active</option>
                <option value="false">Disabled</option>
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
                placeholder="Search application (min 3 chars)"
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
            <Link className="btn btn-primary" to="/applications/new">
              <i className="fa fa-plus"></i> Add New
            </Link>
          </div>
        </div>
        {showInitialLoading ? (
          <div className="text-center py-5">Loading applications...</div>
        ) : (
          <div className="position-relative">
            {showRefreshingState && (
              <div className="px-3 pt-2 text-muted small">
                Refreshing applications...
              </div>
            )}
            {viewMode === "table" ? (
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
                      <th>Client Name</th>
                      <th>Client ID</th>
                      <th>App Type</th>
                      <th>Grant Type</th>
                      <th>Token Type</th>
                      <th>Status</th>
                      <th className="text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {state.items.map((item) => (
                      <tr key={getItemId(item)}>
                        <td className="table-checkbox">
                          <input
                            type="checkbox"
                            checked={selectedIds.has(getItemId(item))}
                            onChange={(event) => {
                              const id = getItemId(item);
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
                        <td>{getClientName(item)}</td>
                        <td className="text-muted">{getClientId(item)}</td>
                        <td>
                          {getLookupLabel(state.appTypes, getAppType(item))}
                        </td>
                        <td>{getGrantTypeLabel(item)}</td>
                        <td>
                          {getLookupLabel(state.tokenTypes, getTokenType(item))}
                        </td>
                        <td>
                          <span
                            className={`status-pill ${
                              isApplicationActive(item)
                                ? "status-pill-success"
                                : "status-pill-off"
                            }`}
                          >
                            {isApplicationActive(item) ? "Active" : "Disabled"}
                          </span>
                        </td>
                        <td className="text-right table-actions">
                          <button
                            className="btn btn-link p-0 text-primary ButtonLink"
                            type="button"
                            onClick={() => {
                              const id = getItemId(item);
                              const clientId = getClientId(item);
                              if (!clientId) {
                                return;
                              }
                              navigate(
                                `edit/${encodeURIComponent(String(clientId))}`,
                                {
                                  state: { id },
                                },
                              );
                            }}
                            title="Edit"
                          >
                            <i className="fa fa-pen"></i>
                          </button>
                          {canDeleteApplications && (
                            <button
                              className="btn btn-link p-0 text-danger ButtonLink"
                              type="button"
                              onClick={() => requestDelete(getItemId(item))}
                              title="Delete"
                            >
                              <i className="fa fa-trash"></i>
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                    {state.items.length === 0 && (
                      <tr>
                        <td colSpan="8" className="text-center text-muted py-4">
                          No applications found.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            ) : state.items.length === 0 ? (
              <div className="text-center text-muted py-4">
                No applications found.
              </div>
            ) : (
              <div className="applications-card-grid">
                {state.items.map((item) => {
                  const id = getItemId(item);
                  const clientId = getClientId(item);
                  const isSelected = selectedIds.has(id);
                  const isActive = isApplicationActive(item);
                  const appTypeLabel = getLookupLabel(
                    state.appTypes,
                    getAppType(item),
                  );
                  const grantTypeLabel = getGrantTypeLabel(item);
                  const tokenTypeLabel = getLookupLabel(
                    state.tokenTypes,
                    getTokenType(item),
                  );

                  return (
                    <div
                      key={id}
                      className={`application-grid-card ${
                        isActive ? "is-active" : ""
                      } ${isSelected ? "is-selected" : ""}`}
                    >
                      <div className="application-grid-card-top">
                        <span
                          className={`status-pill ${
                            isActive
                              ? "status-pill-success"
                              : "status-pill-off"
                          }`}
                        >
                          {isActive ? "Active" : "Disabled"}
                        </span>
                        <label className="application-grid-card-checkbox">
                          <input
                            className="form-check-input"
                            type="checkbox"
                            checked={isSelected}
                            onChange={(event) => {
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
                            aria-label={`Select ${getClientName(item)}`}
                          />
                        </label>
                      </div>
                      <div className="application-grid-card-icon">
                        <div className="application-grid-card-avatar-wrap">
                          <img
                            src={defaultApplicationImage}
                            alt="Application"
                            className="application-grid-card-avatar"
                          />
                        </div>
                      </div>
                      <div className="application-grid-card-title">
                        {getClientName(item)}
                      </div>
                      <div className="application-grid-card-id" title={clientId}>
                        {clientId || "No client ID"}
                      </div>
                      <div className="application-grid-card-meta">
                        <div className="application-grid-card-meta-item">
                          <span className="application-grid-card-meta-label">
                            App Type
                          </span>
                          <span className="application-grid-card-meta-value">
                            {appTypeLabel}
                          </span>
                        </div>
                        <div className="application-grid-card-meta-item">
                          <span className="application-grid-card-meta-label">
                            Grant Type
                          </span>
                          <span className="application-grid-card-meta-value">
                            {grantTypeLabel}
                          </span>
                        </div>
                        <div className="application-grid-card-meta-item">
                          <span className="application-grid-card-meta-label">
                            Token Type
                          </span>
                          <span className="application-grid-card-meta-value">
                            {tokenTypeLabel}
                          </span>
                        </div>
                      </div>
                      <div className="application-grid-card-actions">
                        <button
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          onClick={() => {
                            if (!clientId) {
                              return;
                            }

                            navigate(
                              `edit/${encodeURIComponent(String(clientId))}`,
                              {
                                state: { id },
                              },
                            );
                          }}
                          title="Edit"
                          aria-label="Edit application"
                        >
                          <i className="fa fa-pen"></i>
                        </button>
                        {canDeleteApplications && (
                          <button
                            className="btn btn-link p-0 text-danger ButtonLink"
                            type="button"
                            onClick={() => requestDelete(id)}
                            title="Delete"
                            aria-label="Delete application"
                          >
                            <i className="fa fa-trash"></i>
                          </button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
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
        title="Delete Application"
        message="Are you sure you want to delete this application? This action cannot be undone."
        confirmLabel="Delete"
        onConfirm={confirmDelete}
        onClose={closeConfirm}
      />
    </div>
  );
}

export default ApplicationsList;
