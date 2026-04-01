import React, { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useApplications } from "../../_hooks/useApplications";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import Pagination from "../common/pagination";

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

function ApplicationsList() {
  const { state, loadApplications, loadLookups, deleteApplication } =
    useApplications();
  const navigate = useNavigate();
  const isFirstApplicationsLoad = useRef(true);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [filters, setFilters] = useState({
    appType: "",
    tokenType: "",
    status: "",
    search: "",
  });

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
          <div className="filters-header">Filters</div>
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
              <button className="btn btn-soft dropdown-toggle" type="button">
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
            <div className="table-responsive">
              <table className="table table-hover align-middle table-striped table-bordered">
                <thead>
                  <tr>
                    <th className="table-checkbox">
                      <input type="checkbox" />
                    </th>
                    <th>Client Name</th>
                    <th>Client ID</th>
                    <th>App Type</th>
                    <th>Token Type</th>
                    <th>Status</th>
                    <th className="text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {state.items.map((item) => (
                    <tr key={getField(item, "id", "Id")}>
                      <td className="table-checkbox">
                        <input type="checkbox" />
                      </td>
                      <td>{getField(item, "clientName", "ClientName")}</td>
                      <td className="text-muted">
                        {getField(item, "clientId", "ClientId")}
                      </td>
                      <td>
                        {getLookupLabel(
                          state.appTypes,
                          getField(item, "appType", "AppType"),
                        )}
                      </td>
                      <td>
                        {getLookupLabel(
                          state.tokenTypes,
                          getField(
                            item,
                            "tokenType",
                            "TokenType",
                            "accessTokenType",
                            "AccessTokenType",
                          ),
                        )}
                      </td>
                      <td>
                        <span
                          className={`status-pill ${
                            getField(item, "isActive", "IsActive")
                              ? "status-pill-success"
                              : "status-pill-off"
                          }`}
                        >
                          {getField(item, "isActive", "IsActive")
                            ? "Active"
                            : "Disabled"}
                        </span>
                      </td>
                      <td className="text-right table-actions">
                        <button
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          onClick={() => {
                            const id = getField(item, "id", "Id");
                            const clientId = getField(
                              item,
                              "clientId",
                              "ClientId",
                            );
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
                        <button
                          className="btn btn-link p-0 text-danger ButtonLink"
                          type="button"
                          onClick={() =>
                            requestDelete(getField(item, "id", "Id"))
                          }
                          title="Delete"
                        >
                          <i className="fa fa-trash"></i>
                        </button>
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
