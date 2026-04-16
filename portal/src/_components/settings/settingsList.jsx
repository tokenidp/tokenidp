import React, { useCallback, useEffect, useMemo, useState } from "react";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import InfoModal from "../common/infoModal";
import { useSettings } from "../../_hooks/useSettings";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";
import { useAuth } from "tokenidp-react";

const defaultSearch = {
  pageNumber: 1,
  pageSize: 500,
  sortColumn: "Scope",
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
  { value: 0, label: "System", icon: "fa-sliders" },
  { value: 1, label: "Security", icon: "fa-shield" },
  { value: 2, label: "Notification", icon: "fa-bell" },
  { value: 3, label: "Branding", icon: "fa-palette" },
  { value: 4, label: "Integration", icon: "fa-plug" },
];

const getField = (item, ...keys) => {
  const resolvedKey = keys.find((key) => item?.[key] !== undefined);
  return resolvedKey ? item?.[resolvedKey] : undefined;
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

const normalizeScopeValue = (value) => {
  if (value === null || value === undefined || value === "") {
    return 0;
  }

  if (typeof value === "string") {
    const match = scopeValueOptions.find(
      (option) => option.label.toLowerCase() === value.toLowerCase(),
    );
    return match ? match.value : Number(value);
  }

  return Number(value);
};

const getScopeLabel = (value) =>
  scopeValueOptions.find((option) => option.value === normalizeScopeValue(value))
    ?.label || "System";

const getValueTypeLabel = (value) =>
  valueTypeOptions.find((option) => option.value === normalizeValueType(value))
    ?.label || "String";

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
    .map((permission) =>
      permission?.permissionKey || permission?.PermissionKey || permission?.Key,
    )
    .filter(Boolean)
    .map((permissionKey) => String(permissionKey).trim().toLowerCase());
};

const getDefaultValueForType = (valueType) => {
  switch (normalizeValueType(valueType)) {
    case 1:
      return "0";
    case 2:
      return "false";
    case 4:
      return "{}";
    default:
      return "";
  }
};

const normalizeBooleanValue = (value) =>
  String(value ?? "").trim().toLowerCase() === "true";

const validateConfigurationValue = (valueType, value) => {
  const normalizedType = normalizeValueType(valueType);
  const normalizedValue = String(value ?? "");

  if (!normalizedValue.trim()) {
    return "Value is required.";
  }

  if (normalizedType === 1 && !/^-?\d+$/.test(normalizedValue.trim())) {
    return "Int values must be whole numbers.";
  }

  if (
    normalizedType === 2 &&
    !["true", "false"].includes(normalizedValue.trim().toLowerCase())
  ) {
    return "Bool values must be true or false.";
  }

  if (normalizedType === 4) {
    try {
      JSON.parse(normalizedValue);
    } catch {
      return "Json values must be valid JSON.";
    }
  }

  return "";
};

function SettingsList() {
  const user = useAuth();
  const { state, loadSettings, bulkSave, deleteConfiguration } = useSettings();
  const { setSuccess } = useGlobalSuccess();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [infoOpen, setInfoOpen] = useState(false);
  const [infoContent, setInfoContent] = useState({ title: "", message: "" });
  const [searchTerm, setSearchTerm] = useState("");
  const [activeScope, setActiveScope] = useState(scopeValueOptions[0].value);
  const [drafts, setDrafts] = useState({});
  const [newEntry, setNewEntry] = useState(null);

  const showInitialLoading = !state.hasLoadedSettings;
  const showRefreshingState = state.hasLoadedSettings && state.loadingSettings;
  const permissionKeys = useMemo(() => normalizePermissions(user), [user]);
  const canEditSettings = permissionKeys.includes("settings.edit");
  const canDeleteSettings = permissionKeys.includes("settings.delete");

  const loadAllSettings = useCallback(async () => {
    const initialResult = await loadSettings({
      ...defaultSearch,
      SearchCriterias: [],
    });

    const items = initialResult?.items || initialResult?.Items || [];
    const totalCount =
      initialResult?.totalCount ||
      initialResult?.TotalCount ||
      (Array.isArray(items) ? items.length : 0);

    if (Array.isArray(items) && totalCount > items.length) {
      await loadSettings({
        ...defaultSearch,
        pageSize: totalCount,
        SearchCriterias: [],
      });
    }
  }, [loadSettings]);

  useEffect(() => {
    loadAllSettings();
  }, [loadAllSettings]);

  useEffect(() => {
    if (!newEntry) {
      return;
    }

    setNewEntry((prev) =>
      !prev || prev.scope === activeScope
        ? prev
        : {
            ...prev,
            scope: activeScope,
          },
    );
  }, [activeScope, newEntry]);

  const items = useMemo(
    () =>
      (state.items || [])
        .map((item) => {
          const id = getField(item, "id", "Id");
          const key = String(getField(item, "key", "Key") || "");
          const value = String(getField(item, "value", "Value") ?? "");
          const valueType = normalizeValueType(
            getField(item, "valueType", "ValueType"),
          );
          const scopeValue = normalizeScopeValue(
            getField(item, "scope", "Scope"),
          );
          const isEditable =
            getField(item, "isEditable", "IsEditable") ?? true;

          return {
            id,
            key,
            value,
            valueType,
            valueTypeLabel: getValueTypeLabel(valueType),
            scopeValue,
            scopeLabel: getScopeLabel(scopeValue),
            isEditable: !!isEditable,
          };
        })
        .sort((left, right) => left.key.localeCompare(right.key)),
    [state.items],
  );

  const itemCountsByScope = useMemo(
    () =>
      items.reduce((counts, item) => {
        counts[item.scopeValue] = (counts[item.scopeValue] || 0) + 1;
        return counts;
      }, {}),
    [items],
  );

  const filteredItems = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLowerCase();

    return items.filter((item) => {
      if (item.scopeValue !== activeScope) {
        return false;
      }

      if (!normalizedSearch) {
        return true;
      }

      return item.key.toLowerCase().includes(normalizedSearch);
    });
  }, [activeScope, items, searchTerm]);

  const activeScopeOption =
    scopeValueOptions.find((option) => option.value === activeScope) ||
    scopeValueOptions[0];

  const handleDraftChange = (id, value) => {
    setDrafts((prev) => ({
      ...prev,
      [id]: {
        ...(prev[id] || {}),
        value,
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

  const openInfo = (title, message) => {
    setInfoContent({ title, message });
    setInfoOpen(true);
  };

  const closeInfo = () => setInfoOpen(false);

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
      await loadAllSettings();
    }
  };

  const hasPendingChanges = useMemo(() => {
    const draftKeys = Object.keys(drafts || {});
    return draftKeys.length > 0 || !!newEntry;
  }, [drafts, newEntry]);

  const buildBulkItems = () => {
    const updates = [];

    items.forEach((item) => {
      const draft = drafts[item.id];
      if (!draft) {
        return;
      }

      updates.push({
        Id: item.id,
        Key: item.key,
        Value: draft.value ?? item.value,
        ValueType: item.valueType,
        Scope: item.scopeValue,
        IsEditable: item.isEditable,
      });
    });

    if (newEntry) {
      updates.push({
        Key: newEntry.key.trim(),
        Value: String(newEntry.value ?? ""),
        ValueType: Number(newEntry.valueType),
        Scope: Number(newEntry.scope),
        IsEditable: !!newEntry.isEditable,
      });
    }

    return updates;
  };

  const saveChanges = async () => {
    const payload = buildBulkItems();

    const invalidEntry = payload.find((item) => {
      if (!item.Key?.trim()) {
        return true;
      }

      return !!validateConfigurationValue(item.ValueType, item.Value);
    });

    if (invalidEntry) {
      const message = !invalidEntry.Key?.trim()
        ? "Key is required for every configuration entry."
        : validateConfigurationValue(invalidEntry.ValueType, invalidEntry.Value);

      openInfo("Validation error", message);
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
      await loadAllSettings();
      return;
    }

    openInfo("Save failed", "Unable to save configuration changes.");
  };

  const startNewEntry = () => {
    setNewEntry((prev) =>
      prev || {
        key: "",
        value: "",
        valueType: 0,
        scope: activeScope,
        isEditable: true,
      },
    );
  };

  const updateNewEntry = (field, value) => {
    setNewEntry((prev) => (prev ? { ...prev, [field]: value } : prev));
  };

  const renderValueEditor = (item, value, onChange, disabled = false) => {
    if (item.valueType === 2) {
      return (
        <label className="settings-entry-bool">
          <div className="form-check form-switch app-switch mb-0">
            <input
              className="form-check-input app-switch-input"
              type="checkbox"
              checked={normalizeBooleanValue(value)}
              disabled={disabled}
              onChange={(event) =>
                onChange(event.target.checked ? "true" : "false")
              }
            />
          </div>
          <span className="settings-entry-bool-text">
            {normalizeBooleanValue(value) ? "On" : "Off"}
          </span>
        </label>
      );
    }

    if (item.valueType === 4) {
      return (
        <textarea
          className="form-control font-monospace"
          rows="5"
          disabled={disabled}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      );
    }

    return (
      <input
        className="form-control"
        type={item.valueType === 1 ? "number" : "text"}
        inputMode={item.valueType === 1 ? "numeric" : undefined}
        step={item.valueType === 1 ? "1" : undefined}
        disabled={disabled}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    );
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
        <div className="settings-topbar">
          <div className="table-search settings-search">
            <i className="fa fa-search"></i>
            <input
              type="text"
              className="form-control"
              placeholder={`Search ${activeScopeOption.label.toLowerCase()} keys`}
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
            />
          </div>

          <div className="settings-topbar-actions">
            <div className="scopes-resource-count" aria-live="polite">
              {Object.keys(drafts).length + (newEntry ? 1 : 0)} pending
            </div>
            <button
              className="btn btn-primary"
              type="button"
              onClick={startNewEntry}
              disabled={!canEditSettings}
              title={
                canEditSettings
                  ? "Add configuration entry"
                  : "settings.edit permission is required"
              }
            >
              <i className="fa fa-plus"></i> Add Entry
            </button>
            <button
              className="btn btn-primary"
              type="button"
              disabled={!canEditSettings || !hasPendingChanges || state.saving}
              onClick={saveChanges}
              title={
                canEditSettings
                  ? "Save configuration changes"
                  : "settings.edit permission is required"
              }
            >
              <i className="fa fa-save me-1" aria-hidden="true"></i>
              {state.saving ? "Saving..." : "Save Changes"}
            </button>
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

            <div className="settings-shell">
              <aside className="settings-sidebar" aria-label="Configuration scopes">
                <div className="settings-nav">
                  {scopeValueOptions.map((scope) => {
                    const count = itemCountsByScope[scope.value] || 0;
                    const isActive = scope.value === activeScope;

                    return (
                      <button
                        key={scope.value}
                        type="button"
                        className={`settings-nav-item ${
                          isActive ? "is-active" : ""
                        }`}
                        onClick={() => setActiveScope(scope.value)}
                        aria-current={isActive ? "true" : undefined}
                      >
                        <div className="settings-nav-item-top">
                          <span className="settings-nav-icon" aria-hidden="true">
                            <i className={`fa ${scope.icon}`}></i>
                          </span>
                          <span className="settings-nav-count">{count}</span>
                        </div>
                        <div className="settings-nav-title">{scope.label}</div>
                        <div className="settings-nav-subtitle">
                          {count === 1 ? "1 setting" : `${count} settings`}
                        </div>
                      </button>
                    );
                  })}
                </div>
              </aside>

              <section className="settings-detail">
                <div className="settings-detail-header">
                  <div>
                    <h6 className="wizard-step-title mb-1">
                      {activeScopeOption.label}
                    </h6>
                    <div className="text-muted small">
                      Edit tenant configuration values for the selected scope.
                    </div>
                  </div>
                  <div className="settings-detail-summary">
                    <span className="scopes-resource-count">
                      {filteredItems.length} visible
                    </span>
                  </div>
                </div>

                <div className="settings-entry-list">
                  {filteredItems.map((item) => {
                    const draft = drafts[item.id];
                    const value = draft?.value ?? item.value;

                    return (
                      <div
                        key={item.id || item.key}
                        className={`settings-entry-card ${
                          draft ? "has-draft" : ""
                        } ${item.isEditable ? "" : "is-locked"}`}
                      >
                        <div className="settings-entry-info">
                          <div className="settings-entry-key">{item.key}</div>
                          <div className="settings-entry-caption">
                            Scope: {item.scopeLabel}
                          </div>
                          <div className="settings-entry-meta">
                            <span className="status-pill status-pill-secondary">
                              {item.valueTypeLabel}
                            </span>
                            <span
                              className={`status-pill ${
                                item.isEditable
                                  ? "status-pill-success"
                                  : "status-pill-off"
                              }`}
                            >
                              {item.isEditable ? "Editable" : "Read only"}
                            </span>
                          </div>
                        </div>

                        <div className="settings-entry-field">
                          {renderValueEditor(
                            item,
                            value,
                            (nextValue) => handleDraftChange(item.id, nextValue),
                            !canEditSettings || !item.isEditable || state.saving,
                          )}
                        </div>

                        <div className="settings-entry-actions">
                          {draft && (
                            <button
                              className="btn btn-link p-0 text-secondary ButtonLink"
                              type="button"
                              title="Discard"
                              onClick={() => clearDraft(item.id)}
                            >
                              <i className="fa fa-rotate-left"></i>
                            </button>
                          )}
                          {canDeleteSettings && (
                            <button
                              className="btn btn-link p-0 text-danger ButtonLink"
                              type="button"
                              title="Delete"
                              onClick={() => requestDelete(item.id)}
                              disabled={!item.isEditable || state.saving}
                            >
                              <i className="fa fa-trash"></i>
                            </button>
                          )}
                        </div>
                      </div>
                    );
                  })}

                  {newEntry && newEntry.scope === activeScope && (
                    <div className="settings-entry-card settings-entry-card-new">
                      <div className="settings-entry-info">
                        <div className="settings-entry-key">New Entry</div>
                        <div className="settings-entry-caption">
                          Scope: {activeScopeOption.label}
                        </div>
                        <div className="settings-entry-meta">
                          <span className="status-pill status-pill-warning">
                            Unsaved
                          </span>
                        </div>
                      </div>

                      <div className="settings-entry-field">
                        <div className="settings-new-entry-grid">
                          <div>
                            <label className="form-label">Key</label>
                            <input
                              className="form-control"
                              placeholder="security.mfa.enabled"
                              value={newEntry.key}
                              onChange={(event) =>
                                updateNewEntry("key", event.target.value)
                              }
                            />
                          </div>

                          <div>
                            <label className="form-label">Type</label>
                            <select
                              className="form-select"
                              value={newEntry.valueType}
                              onChange={(event) => {
                                const nextType = Number(event.target.value);
                                updateNewEntry("valueType", nextType);
                                updateNewEntry(
                                  "value",
                                  getDefaultValueForType(nextType),
                                );
                              }}
                            >
                              {valueTypeOptions.map((option) => (
                                <option key={option.value} value={option.value}>
                                  {option.label}
                                </option>
                              ))}
                            </select>
                          </div>

                          <div>
                            <label className="form-label">Editable</label>
                            <label className="settings-entry-bool">
                              <div className="form-check form-switch app-switch mb-0">
                                <input
                                  className="form-check-input app-switch-input"
                                  type="checkbox"
                                  checked={!!newEntry.isEditable}
                                  onChange={(event) =>
                                    updateNewEntry(
                                      "isEditable",
                                      event.target.checked,
                                    )
                                  }
                                />
                              </div>
                              <span className="settings-entry-bool-text">
                                {newEntry.isEditable ? "Yes" : "No"}
                              </span>
                            </label>
                          </div>

                          <div className="settings-new-entry-value">
                            <label className="form-label">Value</label>
                            {renderValueEditor(
                              {
                                valueType: newEntry.valueType,
                              },
                              String(newEntry.value ?? ""),
                              (nextValue) => updateNewEntry("value", nextValue),
                              !canEditSettings,
                            )}
                          </div>
                        </div>
                      </div>

                      <div className="settings-entry-actions">
                        <button
                          className="btn btn-link p-0 text-danger ButtonLink"
                          type="button"
                          title="Remove"
                          onClick={() => setNewEntry(null)}
                        >
                          <i className="fa fa-times"></i>
                        </button>
                      </div>
                    </div>
                  )}

                  {filteredItems.length === 0 &&
                    (!newEntry || newEntry.scope !== activeScope) && (
                      <div className="settings-empty-state">
                        No configurations found for {activeScopeOption.label}.
                      </div>
                    )}
                </div>
              </section>
            </div>
          </div>
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
