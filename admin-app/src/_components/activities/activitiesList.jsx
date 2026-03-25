import React, { useEffect, useState } from "react";
import Breadcrumbs from "../common/breadcrumbs";
import Pagination from "../common/pagination";
import { useActivities } from "../../_hooks/useActivities";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "Timestamp",
  sortOrder: "desc",
  searchAll: false,
};

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const statusBadge = (status) => {
  switch ((status || "").toLowerCase()) {
    case "success":
      return "status-pill-success";
    case "failure":
      return "status-pill-off";
    default:
      return "status-pill-off";
  }
};

const formatLocalDateTime = (value) => {
  if (!value) {
    return "-";
  }
  const raw = String(value);
  const normalized = raw.replace(/\.\d+(?=z$|Z$)/, "");
  const hasTimezone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(normalized);
  const iso = hasTimezone ? normalized : `${normalized}Z`;
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) {
    return raw;
  }
  const parts = new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  }).formatToParts(parsed);
  const get = (type) => parts.find((part) => part.type === type)?.value || "";
  return `${get("month")} ${get("day")}, ${get("year")} ${get("hour")}:${get("minute")} ${get("dayPeriod")}`.trim();
};

function ActivitiesList() {
  const { state, loadActivities, loadLookups } = useActivities();
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [filters, setFilters] = useState({
    startDate: "",
    endDate: "",
    eventType: "",
    actorType: "",
    status: "",
    search: "",
  });

  const totalCount = state.totalCount || 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  const buildSearchCriterias = () => {
    const criterias = [];
    if (filters.search.trim()) {
      const term = filters.search.trim();
      if (term.length >= 3) {
        criterias.push({
          columnName: "Search",
          value: term,
          columnType: 1,
        });
      }
    }
    if (filters.startDate) {
      criterias.push({
        columnName: "StartDate",
        value: filters.startDate,
        columnType: 3,
      });
    }
    if (filters.endDate) {
      criterias.push({
        columnName: "EndDate",
        value: filters.endDate,
        columnType: 3,
      });
    }
    if (filters.eventType) {
      criterias.push({
        columnName: "EventType",
        value: filters.eventType,
        columnType: 1,
      });
    }
    if (filters.actorType) {
      criterias.push({
        columnName: "ActorType",
        value: filters.actorType,
        columnType: 1,
      });
    }
    if (filters.status) {
      criterias.push({
        columnName: "Status",
        value: filters.status,
        columnType: 1,
      });
    }
    return criterias;
  };

  useEffect(() => {
    const trimmedSearch = filters.search.trim();
    if (trimmedSearch.length > 0 && trimmedSearch.length < 3) {
      return () => {};
    }

    const timeout = setTimeout(() => {
      loadActivities({
        ...defaultSearch,
        pageNumber,
        pageSize,
        searchCriterias: buildSearchCriterias(),
      });
    }, 400);

    return () => clearTimeout(timeout);
  }, [loadActivities, pageNumber, pageSize, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Activities</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header">Filters</div>
          <div className="filters-grid">
            <div className="filter-field">
              <label className="form-label">Date Range</label>
              <div className="d-flex gap-2">
                <input
                  className="form-control"
                  type="date"
                  value={filters.startDate}
                  onChange={(event) => {
                    setFilters((prev) => ({
                      ...prev,
                      startDate: event.target.value,
                    }));
                    setPageNumber(1);
                  }}
                />
                <input
                  className="form-control"
                  type="date"
                  value={filters.endDate}
                  onChange={(event) => {
                    setFilters((prev) => ({
                      ...prev,
                      endDate: event.target.value,
                    }));
                    setPageNumber(1);
                  }}
                />
              </div>
            </div>
            <div className="filter-field">
              <label className="form-label">Event Type</label>
              <select
                className="form-select"
                value={filters.eventType}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    eventType: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Events</option>
                {state.eventTypes.map((option) => (
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
              <label className="form-label">Actor</label>
              <select
                className="form-select"
                value={filters.actorType}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    actorType: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Actors</option>
                {state.actorTypes.map((option) => (
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
                <option value="Success">Success</option>
                <option value="Failure">Failure</option>
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
                placeholder="Search activities"
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
          </div>
        </div>

        {state.loading ? (
          <div className="text-center py-5">Loading activities...</div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover align-middle table-striped table-bordered">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Event Type</th>
                  <th>Actor</th>
                  <th>Target</th>
                  <th>Description</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {state.items.map((item) => {
                  const timestamp = getField(item, "timestamp", "Timestamp");
                  const eventType = getField(item, "eventType", "EventType");
                  const actor = getField(item, "actor", "Actor");
                  const target = getField(item, "target", "Target");
                  const description = getField(
                    item,
                    "description",
                    "Description",
                  );
                  const status = getField(item, "status", "Status");

                  return (
                    <tr key={`${timestamp}-${eventType}-${actor}`}>
                      <td>
                        {formatLocalDateTime(timestamp) || "2025-01-10 12:42"}
                      </td>
                      <td>
                        <i
                          className={`fa ${
                            eventType === "Login"
                              ? "fa-right-to-bracket"
                              : eventType === "Token"
                                ? "fa-key"
                                : "fa-shield"
                          } me-2 text-secondary`}
                        ></i>
                        {eventType || "Login"}
                      </td>
                      <td>{actor || "user@tenant.com"}</td>
                      <td>{target || "Tenant A"}</td>
                      <td className="text-muted">
                        {description || "User login succeeded."}
                      </td>
                      <td>
                        <span className={`status-pill ${statusBadge(status)}`}>
                          {status || "Success"}
                        </span>
                      </td>
                    </tr>
                  );
                })}
                {state.items.length === 0 && (
                  <tr>
                    <td colSpan="6" className="text-center text-muted py-4">
                      No activities found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        <Pagination
          pageNumber={pageNumber}
          pageSize={pageSize}
          totalCount={totalCount}
          onPageChange={setPageNumber}
        />
      </div>
    </div>
  );
}

export default ActivitiesList;