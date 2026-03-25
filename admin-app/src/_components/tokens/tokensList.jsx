import React, { useEffect, useMemo, useState } from "react";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import Pagination from "../common/pagination";
import { useTokens } from "../../_hooks/useTokens";
import TokenInspectModal from "./tokenInspectModal";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "IssuedAt",
  sortOrder: "desc",
  searchAll: false,
};

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const statusBadge = (status) => {
  const normalized = String(status ?? "").toLowerCase();
  switch (normalized) {
    case "active":
    case "0":
      return "status-pill-success";
    case "revoked":
    case "2":
      return "status-pill-off";
    case "expired":
    case "1":
      return "status-pill-off";
    case "compromised":
    case "3":
      return "status-pill-danger";
    case "suspended":
    case "4":
      return "status-pill-warning";
    default:
      return "status-pill-off";
  }
};

const formatLocalDateTime = (value) => {
  if (!value) {
    return "-";
  }
  const raw = String(value);
  const hasTimezone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(raw);
  const iso = hasTimezone ? raw : `${raw}Z`;
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

function TokensList() {
  const {
    state,
    loadTokens,
    loadLookups,
    getTokenById,
    revokeToken,
    expireToken,
  } = useTokens();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmAction, setConfirmAction] = useState(null);
  const [inspectOpen, setInspectOpen] = useState(false);
  const [inspectToken, setInspectToken] = useState(null);
  const [inspectLoading, setInspectLoading] = useState(false);
  const [inspectTab, setInspectTab] = useState("overview");
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [sortColumn, setSortColumn] = useState(defaultSearch.sortColumn);
  const [sortOrder, setSortOrder] = useState(defaultSearch.sortOrder);
  const [filters, setFilters] = useState({
    sourceType: "",
    clientId: "",
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
    if (filters.sourceType) {
      criterias.push({
        columnName: "SourceType",
        value: filters.sourceType,
        columnType: 1,
      });
    }
    if (filters.clientId) {
      criterias.push({
        columnName: "ClientId",
        value: filters.clientId,
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
      loadTokens({
        ...defaultSearch,
        pageNumber,
        pageSize,
        sortColumn,
        sortOrder,
        searchCriterias: buildSearchCriterias(),
      });
    }, 400);

    return () => clearTimeout(timeout);
  }, [loadTokens, pageNumber, pageSize, sortColumn, sortOrder, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  const requestAction = (action, token) => {
    setConfirmAction({ action, token });
    setConfirmOpen(true);
  };

  const closeConfirm = () => {
    setConfirmOpen(false);
    setConfirmAction(null);
  };

  const confirmActionRequest = async () => {
    if (!confirmAction?.token) {
      closeConfirm();
      return;
    }
    const tokenId = getField(confirmAction.token, "tokenId", "TokenId");
    if (!tokenId) {
      closeConfirm();
      return;
    }

    if (confirmAction.action === "revoke") {
      await revokeToken(tokenId);
    } else if (confirmAction.action === "expire") {
      await expireToken(tokenId);
    }

    closeConfirm();
    loadTokens({
      ...defaultSearch,
      pageNumber,
      pageSize,
      sortColumn,
      sortOrder,
      searchCriterias: buildSearchCriterias(),
    });
  };

  const handleInspect = async (token, tab = "overview") => {
    const tokenId = getField(token, "tokenId", "TokenId");
    setInspectTab(tab);
    setInspectOpen(true);
    setInspectToken(token || null);
    if (tokenId === undefined || tokenId === null || tokenId === "") {
      return;
    }
    setInspectLoading(true);
    const detail = await getTokenById(tokenId);
    if (detail) {
      setInspectToken(detail);
    }
    setInspectLoading(false);
  };

  const closeInspect = () => {
    setInspectOpen(false);
    setInspectToken(null);
  };

  const handleSort = (column) => {
    if (sortColumn === column) {
      setSortOrder((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortColumn(column);
      setSortOrder("asc");
    }
    setPageNumber(1);
  };

  const statusLabel = useMemo(() => {
    const map = {
      0: "Active",
      1: "Expired",
      2: "Revoked",
      3: "Compromised",
      4: "Suspended",
    };
    return (value) =>
      typeof value === "number"
        ? map[value] || "Unknown"
        : map[Number(value)] || value || "Unknown";
  }, []);

  const renderSkeletonRows = () =>
    Array.from({ length: Math.min(pageSize, 6) }).map((_, index) => (
      <tr key={`skeleton-${index}`} className="placeholder-glow">
        <td>
          <span className="placeholder col-6"></span>
        </td>
        <td>
          <span className="placeholder col-4"></span>
        </td>
        <td>
          <span className="placeholder col-6"></span>
        </td>
        <td>
          <span className="placeholder col-6"></span>
        </td>
        <td>
          <span className="placeholder col-5"></span>
        </td>
        <td>
          <span className="placeholder col-5"></span>
        </td>
        <td>
          <span className="placeholder col-4"></span>
        </td>
        <td className="text-right">
          <span className="placeholder col-6"></span>
        </td>
      </tr>
    ));

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Tokens</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="filters-panel">
          <div className="filters-header">Filters</div>
          <div className="filters-grid tokens-filters-grid">
            <div className="filter-field">
              <label className="form-label">Source Type</label>
              <select
                className="form-select"
                value={filters.sourceType}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    sourceType: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Source Types</option>
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
              <label className="form-label">Client</label>
              <select
                className="form-select"
                value={filters.clientId}
                onChange={(event) => {
                  setFilters((prev) => ({
                    ...prev,
                    clientId: event.target.value,
                  }));
                  setPageNumber(1);
                }}
              >
                <option value="">All Clients</option>
                {state.clients.map((option) => (
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
            <div className="table-search tokens-table-search">
              <i className="fa fa-search"></i>
              <input
                type="text"
                className="form-control"
                placeholder="Search by token ID or user name (min 3 chars)"
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
          <div className="table-responsive">
            <table className="table table-hover align-middle table-striped">
              <thead>
                <tr>
                  <th>Token ID</th>
                  <th>Type</th>
                  <th>Client</th>
                  <th>User</th>
                  <th>Issued At</th>
                  <th>Expires At</th>
                  <th>Status</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>{renderSkeletonRows()}</tbody>
            </table>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover align-middle table-striped table-bordered">
              <thead>
                <tr>
                  <th
                    className="col-token-id"
                    role="button"
                    onClick={() => handleSort("TokenId")}
                  >
                    Token ID
                  </th>
                  <th
                    className="col-token-type"
                    role="button"
                    onClick={() => handleSort("SourceType")}
                  >
                    Source Type
                  </th>
                  <th
                    className="col-token-client"
                    role="button"
                    onClick={() => handleSort("ClientName")}
                  >
                    Client
                  </th>
                  <th
                    className="col-token-user"
                    role="button"
                    onClick={() => handleSort("UserName")}
                  >
                    User
                  </th>
                  <th role="button" onClick={() => handleSort("IssuedAt")}>
                    Issued At
                  </th>
                  <th role="button" onClick={() => handleSort("ExpiresAt")}>
                    Expires At
                  </th>
                  <th role="button" onClick={() => handleSort("Status")}>
                    Status
                  </th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {state.items.map((item) => {
                  const tokenId = getField(item, "tokenId", "TokenId");
                  const sourceType = getField(item, "sourceType", "SourceType");
                  const clientName = getField(item, "clientName", "ClientName");
                  const clientId = getField(item, "clientId", "ClientId");
                  const userName = getField(item, "userName", "UserName");
                  const subject = getField(item, "subject", "Subject");
                  const issuedAt = getField(item, "issuedAt", "IssuedAt");
                  const expiresAt = getField(item, "expiresAt", "ExpiresAt");
                  const status = getField(item, "status", "Status");

                  const normalizedSourceType = String(
                    sourceType || "",
                  ).toLowerCase();
                  const normalizedStatus = String(status ?? "").toLowerCase();
                  const isRevoked =
                    normalizedStatus === "2" || normalizedStatus === "revoked";
                  const supportsRevocation =
                    normalizedSourceType === "refresh" ||
                    normalizedSourceType === "referencetoken" ||
                    normalizedSourceType === "reference";
                  const canManageToken = supportsRevocation && !isRevoked;

                  return (
                    <tr key={tokenId}>
                      <td className="text-muted col-token-id">
                        {(tokenId || "tok_****").toString().slice(0, 8)}...
                      </td>
                      <td className="col-token-type">{sourceType || "JWT"}</td>
                      <td className="col-token-client">
                        {clientName
                          ? clientId
                            ? `${clientName} (${clientId})`
                            : clientName
                          : clientId || "Client"}
                      </td>
                      <td className="col-token-user">
                        {userName || subject || "user@tenant.com"}
                      </td>
                      <td>{formatLocalDateTime(issuedAt)}</td>
                      <td>{formatLocalDateTime(expiresAt)}</td>
                      <td>
                        <span className={`status-pill ${statusBadge(status)}`}>
                          {statusLabel(status)}
                        </span>
                      </td>
                      <td className="text-right table-actions">
                        <button
                          className="btn btn-link p-0 text-primary ButtonLink"
                          type="button"
                          onClick={() => handleInspect(item, "overview")}
                          title="Inspect"
                        >
                          <i className="fa fa-eye"></i>
                        </button>
                        <button
                          className="btn btn-link p-0 text-danger ButtonLink"
                          type="button"
                          onClick={() =>
                            canManageToken && requestAction("revoke", item)
                          }
                          title={
                            !supportsRevocation
                              ? "Revocation not supported for this token type"
                              : isRevoked
                                ? "Token already revoked"
                                : "Revoke"
                          }
                          disabled={!canManageToken}
                        >
                          <i className="fa fa-ban"></i>
                        </button>
                        <button
                          className="btn btn-link p-0 text-warning ButtonLink"
                          type="button"
                          onClick={() =>
                            canManageToken && requestAction("expire", item)
                          }
                          title={
                            !supportsRevocation
                              ? "Force expire not supported for this token type"
                              : isRevoked
                                ? "Token already revoked"
                                : "Force Expire"
                          }
                          disabled={!canManageToken}
                        >
                          <i className="fa fa-clock"></i>
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {state.items.length === 0 && (
                  <tr>
                    <td colSpan="8" className="text-center text-muted py-4">
                      No tokens found.
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

      <ConfirmModal
        open={confirmOpen}
        title={
          confirmAction?.action === "expire"
            ? "Force Expire Token"
            : "Revoke Token"
        }
        message={
          confirmAction?.action === "expire"
            ? "Force expire this token immediately? Active sessions may be interrupted."
            : "Revoke this token? The user will need to reauthenticate."
        }
        confirmLabel={confirmAction?.action === "expire" ? "Expire" : "Revoke"}
        onConfirm={confirmActionRequest}
        onClose={closeConfirm}
      />
      <TokenInspectModal
        open={inspectOpen}
        token={inspectToken}
        onClose={closeInspect}
        initialTab={inspectTab}
      />
      {inspectOpen && inspectLoading && (
        <div className="text-center text-muted small mt-2">
          Loading token details...
        </div>
      )}
    </div>
  );
}

export default TokensList;