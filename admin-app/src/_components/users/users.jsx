import React, { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import Pagination from "../common/pagination";
import { useUsers } from "../../_hooks/useUsers";

function Users() {
  const navigate = useNavigate();
  const { state, loadUsers, loadLookups } = useUsers();

  const defaultSearch = {
    pageNumber: 1,
    pageSize: 10,
    sortColumn: "FullName",
    sortOrder: "desc",
    searchAll: false,
  };
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [filters, setFilters] = useState({
    email: "",
    phone: "",
    role: "",
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

  useEffect(() => {
    const timeout = setTimeout(() => {
      const hasShortTextFilter = ["email", "phone", "search"].some((key) => {
        const value = filters[key].trim();
        return value.length > 0 && value.length < 3;
      });

      if (hasShortTextFilter) {
        return;
      }

      loadUsers({
        ...defaultSearch,
        pageNumber,
        pageSize,
        SearchCriterias: buildSearchCriterias(),
      });
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

  const getStatusLabel = (item) => {
    const isActive = getField(item, "isActive", "IsActive");
    if (typeof isActive === "boolean") {
      return isActive ? "Active" : "Disabled";
    }
    return getField(item, "status", "Status") || "Unknown";
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
              <button className="btn btn-soft dropdown-toggle" type="button">
                <i className="fa fa-download"></i> Export
              </button>
            </div>
            <Link className="btn btn-primary-solid" to="new">
              <i className="fa fa-plus"></i> Add New
            </Link>
          </div>
        </div>

        {state.loading ? (
          <div className="text-center py-5">Loading users...</div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover align-middle">
              <thead>
                <tr>
                  <th className="table-checkbox">
                    <input type="checkbox" />
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
                      <input type="checkbox" />
                    </td>
                    <td>{getField(item, "fullName", "name", "Name")}</td>
                    <td className="text-muted col-email">
                      {getField(item, "email", "Email", "userName", "UserName")}
                    </td>
                    <td className="col-phone">
                      {getField(item, "phoneNumber", "PhoneNumber")}
                    </td>
                    <td className="col-roles">{getField(item, "roles", "Roles")}</td>
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
                        onClick={() =>
                          navigate(`edit/${getField(item, "id", "Id")}`)
                        }
                        title="Edit"
                      >
                        <i className="fa fa-pen"></i>
                      </button>
                      <button
                        className="btn btn-link p-0 text-danger ButtonLink"
                        type="button"
                        title="Delete"
                      >
                        <i className="fa fa-trash"></i>
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

export default Users;
