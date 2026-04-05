import React, { useEffect, useMemo, useState } from "react";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import InfoModal from "../common/infoModal";
import Pagination from "../common/pagination";
import { useSettings } from "../../_hooks/useSettings";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 10,
  sortColumn: "ConfigKey",
  sortOrder: "asc",
  searchAll: false,
};

const valueTypeOptions = [
  { value: 0, label: "String" },
  { value: 1, label: "Int" },
  { value: 2, label: "Bool" },
  { value: 4, label: "Json" },
];

const scopeValueOptions = [
  { value: 0, label: "System" },
  { value: 1, label: "Security" },
  { value: 2, label: "Notification" },
  { value: 3, label: "Branding" },
  { value: 4, label: "Integration" },
];

const scopeFilterOptions = [
  { value: "", label: "All scopes" },
  { value: "System", label: "System" },
  { value: "Security", label: "Security" },
  { value: "Notification", label: "Notification" },
  { value: "Branding", label: "Branding" },
  { value: "Integration", label: "Integration" },
];

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const normalizeScopeValue = (value) => {
  if (value === null || value === undefined || value === "") {
    return null;
  }
  if (typeof value === "string") {
    const match = scopeValueOptions.find(
      (option) => option.label.toLowerCase() === value.toLowerCase(),
    );
    return match ? match.value : null;
  }
  return Number(value);
};

const resolveScopeLabel = (value) => {
  if (value === null || value === undefined || value === "") {
    return "";
  }
  if (typeof value === "string") {
    return value;
  }
  return (
    scopeValueOptions.find((option) => option.value === Number(value))?.label ||
    ""
  );
};

const normalizeValueType = (value) => {
  if (value === null || value === undefined || value === "") {
    return 0;
  }
  if (typeof value === "string") {
    const match = valueTypeOptions.find(
      (option) => option.label.toLowerCase() === value.toLowerCase(),
    );
    return match ? match.value : Number(value);
  }
  return Number(value);
};

function SettingsList() {
  const { state, loadSettings, bulkSave, deleteConfiguration } = useSettings();
  const { setSuccess } = useGlobalSuccess();
  const isFirstSettingsLoad = React.useRef(true);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [infoOpen, setInfoOpen] = useState(false);
  const [infoContent, setInfoContent] = useState({ title: "", message: "" });
  const [pageNumber, setPageNumber] = useState(defaultSearch.pageNumber);
  const [pageSize, setPageSize] = useState(defaultSearch.pageSize);
  const [filters, setFilters] = useState({ search: "", scope: "" });
  const [drafts, setDrafts] = useState({});
  const [newEntry, setNewEntry] = useState(null);

  const totalCount = state.totalCount || 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const showInitialLoading = !state.hasLoadedSettings;
  const showRefreshingState = state.hasLoadedSettings && state.loadingSettings;

  const buildSearchCriterias = () => {
    const criterias = [];
    if (filters.search.trim()) {
      criterias.push({
        ColumnName: "Search",
        Value: filters.search.trim(),
        ColumnType: 1,
      });
    }
    if (filters.scope) {
      criterias.push({
        ColumnName: "Scope",
        Value: filters.scope,
        ColumnType: 1,
      });
    }
    return criterias;
  };

  const reload = () =>
    loadSettings({
      ...defaultSearch,
      pageNumber,
      pageSize,
      SearchCriterias: buildSearchCriterias(),
    });

  useEffect(() => {
    const hasShortSearch =
      filters.search.trim().length > 0 && filters.search.trim().length < 3;

    if (hasShortSearch) {
      return () => {};
    }

    if (isFirstSettingsLoad.current) {
      isFirstSettingsLoad.current = false;
      reload();
      return () => {};
    }

    const timeout = setTimeout(() => {
      reload();
    }, 250);

    return () => clearTimeout(timeout);
  }, [loadSettings, pageNumber, pageSize, filters]);

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages);
    }
  }, [pageNumber, totalPages]);

  const items = useMemo(() => state.items || [], [state.items]);

  const handleDraftChange = (id, field, value) => {
    setDrafts((prev) => ({
      ...prev,
      [id]: {
        ...(prev[id] || {}),
        [field]: value,
      },
    }));
  };

  const clearDraft = (id) => {
    setDrafts((prev) => {
      const next = { ...prev };
      delete next[id];
      return next;
    });
  };

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

    const result = await deleteConfiguration(pendingDeleteId);
    closeConfirm();

    if (result.ok) {
      reload();
    }
  };

  const openInfo = (title, message) => {
    setInfoContent({ title, message });
    setInfoOpen(true);
  };

  const closeInfo = () => setInfoOpen(false);

  const hasPendingChanges = useMemo(() => {
    const draftKeys = Object.keys(drafts || {});
    return draftKeys.length > 0 || !!newEntry;
  }, [drafts, newEntry]);

  const buildBulkItems = () => {
    const updates = [];
    items.forEach((item) => {
      const id = getField(item, "id", "Id");
      const draft = drafts[id];
      if (!draft) {
        return;
      }

      const key = getField(item, "key", "Key");
      const value = draft.value ?? getField(item, "value", "Value") ?? "";
      const valueType = normalizeValueType(
        draft.valueType ?? getField(item, "valueType", "ValueType"),
      );
      const scope =
        draft.scope ?? normalizeScopeValue(getField(item, "scope", "Scope"));
      const isEditable =
        draft.isEditable ?? getField(item, "isEditable", "IsEditable") ?? true;

      updates.push({
        Id: id,
        Key: key,
        Value: value,
        ValueType: Number(valueType),
        Scope: scope,
        IsEditable: !!isEditable,
      });
    });

    if (newEntry) {
      updates.push({
        Key: newEntry.key?.trim() || "",
        Value: newEntry.value ?? "",
        ValueType: Number(newEntry.valueType ?? 0),
        Scope: normalizeScopeValue(newEntry.scope),
        IsEditable: !!newEntry.isEditable,
      });
    }

    return updates;
  };

  const saveChanges = async () => {
    const payload = buildBulkItems();
    const invalid = payload.find((item) => !item.Key || !item.Value);
    if (invalid) {
      openInfo(
        "Validation error",
        "Key and value are required for all entries.",
      );
      return;
    }

    const result = await bulkSave(payload);
    if (result.ok) {
      setSuccess({
        title: "Changes saved",
        message: "Tenant configuration updated successfully.",
      });
      setDrafts({});
      setNewEntry(null);
      reload();
      return;
    }

    openInfo("Save failed", "Unable to save configuration changes.");
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Tenant Configuration</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="alert alert-warning">
        Changes apply per tenant and may affect authentication, branding, or
        integrations.
      </div>

      <div className="card-surface applications-card">
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
          <div className="table-toolbar-actions settings-toolbar-actions">
            <div className="table-search">
              <i className="fa fa-search"></i>
              <input
                type="text"
                className="form-control"
                placeholder="Search keys (min 3 chars)"
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
            <select
              className="form-select settings-scope-select"
              value={filters.scope}
              onChange={(event) => {
                setFilters((prev) => ({
                  ...prev,
                  scope: event.target.value,
                }));
                setPageNumber(1);
              }}
            >
              {scopeFilterOptions.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <div className="settings-toolbar-buttons">
              <button
                className="btn btn-primary"
                type="button"
                onClick={() =>
                  setNewEntry({
                    key: "",
                    value: "",
                    valueType: 0,
                    scope: null,
                    isEditable: true,
                  })
                }
              >
                <i className="fa fa-plus"></i> Add Entry
              </button>
              <button
                className="btn btn-primary"
                type="button"
                disabled={!hasPendingChanges || state.saving}
                onClick={saveChanges}
              >
                <i className="fa fa-save me-1" aria-hidden="true"></i>
                {state.saving ? "Saving..." : "Save Changes"}
              </button>
            </div>
          </div>
        </div>

        {showInitialLoading ? (
          <div className="text-center py-5">Loading configurations...</div>
        ) : (
          <div className="position-relative">
            {showRefreshingState && (
              <div className="px-3 pt-2 text-muted small">
                Refreshing configurations...
              </div>
            )}
            <div className="table-responsive">
            <table className="table table-hover align-middle table-striped table-bordered">
              <thead>
                <tr>
                  <th>Key</th>
                  <th>Value</th>
                  <th>Type</th>
                  <th>Scope</th>
                  <th>Editable</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {newEntry && (
                  <tr>
                    <td>
                      <input
                        className="form-control"
                        placeholder="security.mfa.enabled"
                        value={newEntry.key}
                        onChange={(event) =>
                          setNewEntry((prev) => ({
                            ...prev,
                            key: event.target.value,
                          }))
                        }
                      />
                    </td>
                    <td>
                      <input
                        className="form-control"
                        placeholder="true"
                        value={newEntry.value}
                        onChange={(event) =>
                          setNewEntry((prev) => ({
                            ...prev,
                            value: event.target.value,
                          }))
                        }
                      />
                    </td>
                    <td>
                      <select
                        className="form-select"
                        value={newEntry.valueType}
                        onChange={(event) =>
                          setNewEntry((prev) => ({
                            ...prev,
                            valueType: Number(event.target.value),
                          }))
                        }
                      >
                        {valueTypeOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <select
                        className="form-select"
                        value={newEntry.scope ?? ""}
                        onChange={(event) =>
                          setNewEntry((prev) => ({
                            ...prev,
                            scope:
                              event.target.value === ""
                                ? null
                                : Number(event.target.value),
                          }))
                        }
                      >
                        <option value="">None</option>
                        {scopeValueOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <div className="form-check form-switch app-switch">
                        <input
                          className="form-check-input app-switch-input"
                          type="checkbox"
                          checked={!!newEntry.isEditable}
                          onChange={(event) =>
                            setNewEntry((prev) => ({
                              ...prev,
                              isEditable: event.target.checked,
                            }))
                          }
                        />
                      </div>
                    </td>
                    <td className="text-right table-actions">
                      <button
                        className="btn btn-link p-0 text-danger ButtonLink"
                        type="button"
                        onClick={() => setNewEntry(null)}
                        title="Remove"
                      >
                        <i className="fa fa-times"></i>
                      </button>
                    </td>
                  </tr>
                )}
                {items.map((item) => {
                  const id = getField(item, "id", "Id");
                  const key = getField(item, "key", "Key");
                  const valueType = normalizeValueType(
                    getField(item, "valueType", "ValueType"),
                  );
                  const scope = getField(item, "scope", "Scope");
                  const isEditable =
                    getField(item, "isEditable", "IsEditable") ?? true;
                  const draft = drafts[id] || {};
                  const effectiveValueType = normalizeValueType(
                    draft.valueType ?? valueType,
                  );
                  const effectiveScope =
                    draft.scope ?? normalizeScopeValue(scope);

                  return (
                    <tr key={id || key}>
                      <td className="text-muted">{key}</td>
                      <td>
                        {effectiveValueType === 2 ? (
                          <div className="form-check form-switch app-switch">
                            <input
                              className="form-check-input app-switch-input"
                              type="checkbox"
                              checked={
                                String(
                                  draft.value ??
                                    getField(item, "value", "Value"),
                                ).toLowerCase() === "true"
                              }
                              disabled={!isEditable}
                              onChange={(event) =>
                                handleDraftChange(
                                  id,
                                  "value",
                                  event.target.checked ? "true" : "false",
                                )
                              }
                            />
                          </div>
                        ) : effectiveValueType === 4 ? (
                          <textarea
                            className="form-control font-monospace"
                            rows="2"
                            disabled={!isEditable}
                            value={
                              draft.value ?? getField(item, "value", "Value")
                            }
                            onChange={(event) =>
                              handleDraftChange(id, "value", event.target.value)
                            }
                          />
                        ) : (
                          <input
                            className="form-control"
                            type={effectiveValueType === 1 ? "number" : "text"}
                            disabled={!isEditable}
                            value={
                              draft.value ?? getField(item, "value", "Value")
                            }
                            onChange={(event) =>
                              handleDraftChange(id, "value", event.target.value)
                            }
                          />
                        )}
                      </td>
                      <td>
                        <select
                          className="form-select"
                          disabled={!isEditable}
                          value={effectiveValueType}
                          onChange={(event) =>
                            handleDraftChange(
                              id,
                              "valueType",
                              Number(event.target.value),
                            )
                          }
                        >
                          {valueTypeOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <select
                          className="form-select"
                          disabled={!isEditable}
                          value={effectiveScope ?? ""}
                          onChange={(event) =>
                            handleDraftChange(
                              id,
                              "scope",
                              event.target.value === ""
                                ? null
                                : Number(event.target.value),
                            )
                          }
                        >
                          <option value="">
                            {resolveScopeLabel(scope) || "None"}
                          </option>
                          {scopeValueOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <span
                          className={`status-pill ${
                            isEditable
                              ? "status-pill-success"
                              : "status-pill-off"
                          }`}
                        >
                          {isEditable ? "Yes" : "No"}
                        </span>
                      </td>
                      <td className="text-right table-actions">
                        {drafts[id] && (
                          <button
                            className="btn btn-link p-0 text-secondary ButtonLink"
                            type="button"
                            title="Discard"
                            onClick={() => clearDraft(id)}
                          >
                            <i className="fa fa-rotate-left"></i>
                          </button>
                        )}
                        <button
                          className="btn btn-link p-0 text-danger ButtonLink"
                          type="button"
                          title="Delete"
                          onClick={() => requestDelete(id)}
                          disabled={!isEditable}
                        >
                          <i className="fa fa-trash"></i>
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {items.length === 0 && !newEntry && (
                  <tr>
                    <td colSpan="6" className="text-center text-muted py-4">
                      No configurations found.
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
        title="Delete Configuration"
        message="Delete this configuration entry?"
        confirmLabel="Delete"
        onConfirm={confirmDelete}
        onClose={closeConfirm}
      />

      <InfoModal
        open={infoOpen}
        title={infoContent.title}
        message={infoContent.message}
        onClose={closeInfo}
      />
    </div>
  );
}

export default SettingsList;
