import React, { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import { useApiResources } from "../../_hooks/useApiResources";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

const emptyScope = () => ({
  id: null,
  name: "",
  displayName: "",
  description: "",
  enabled: true,
});

const normalizeScope = (scope, clientKey) => ({
  clientKey,
  id: scope.id ?? scope.Id ?? null,
  name: scope.name ?? scope.Name ?? "",
  displayName: scope.displayName ?? scope.DisplayName ?? "",
  description: scope.description ?? scope.Description ?? "",
  enabled: scope.enabled ?? scope.Enabled ?? true,
});

function AddEditApiResource({ mode = "add" }) {
  const navigate = useNavigate();
  const { id } = useParams();
  const { state, getApiResourceById, createApiResource, updateApiResource } = useApiResources();
  const { setSuccess } = useGlobalSuccess();
  const scopeKeyRef = useRef(0);
  const [scopes, setScopes] = useState([]);
  const [scopeDraft, setScopeDraft] = useState(emptyScope());
  const [editingScopeKey, setEditingScopeKey] = useState(null);
  const [scopeErrors, setScopeErrors] = useState({});

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm({
    defaultValues: {
      id: "",
      name: "",
      displayName: "",
      description: "",
      enabled: true,
    },
  });

  const getNextScopeKey = () => {
    scopeKeyRef.current += 1;
    return `scope-${scopeKeyRef.current}`;
  };

  const resetScopeDraft = () => {
    setScopeDraft(emptyScope());
    setEditingScopeKey(null);
    setScopeErrors({});
  };

  useEffect(() => {
    if (mode !== "edit" || !id) {
      return;
    }

    const load = async () => {
      const data = await getApiResourceById(id);
      if (!data) {
        return;
      }

      setValue("id", data.id ?? data.Id ?? "");
      setValue("name", data.name ?? data.Name ?? "");
      setValue("displayName", data.displayName ?? data.DisplayName ?? "");
      setValue("description", data.description ?? data.Description ?? "");
      setValue("enabled", data.enabled ?? data.Enabled ?? true);
      const nextScopes = data.scopes ?? data.Scopes ?? [];
      setScopes(nextScopes.map((scope) => normalizeScope(scope, getNextScopeKey())));
      resetScopeDraft();
    };

    load();
  }, [getApiResourceById, id, mode, setValue]);

  const updateScopeDraft = (field, value) => {
    setScopeDraft((prev) => ({ ...prev, [field]: value }));
    setScopeErrors((prev) => {
      if (!prev[field]) {
        return prev;
      }
      const next = { ...prev };
      delete next[field];
      return next;
    });
  };

  const validateScopeDraft = () => {
    const nextErrors = {};

    if (!scopeDraft.name.trim()) {
      nextErrors.name = "Scope name is required.";
    }

    if (!scopeDraft.displayName.trim()) {
      nextErrors.displayName = "Display name is required.";
    }

    setScopeErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const saveScope = () => {
    if (!validateScopeDraft()) {
      return;
    }

    const normalizedDraft = {
      ...scopeDraft,
      name: scopeDraft.name.trim(),
      displayName: scopeDraft.displayName.trim(),
      description: scopeDraft.description.trim(),
    };

    setScopes((prev) => {
      if (editingScopeKey) {
        return prev.map((scope) =>
          scope.clientKey === editingScopeKey ? { ...scope, ...normalizedDraft } : scope
        );
      }

      return [
        ...prev,
        {
          ...normalizedDraft,
          clientKey: getNextScopeKey(),
        },
      ];
    });

    resetScopeDraft();
  };

  const editScope = (clientKey) => {
    const scope = scopes.find((item) => item.clientKey === clientKey);
    if (!scope) {
      return;
    }

    setScopeDraft({
      id: scope.id,
      name: scope.name,
      displayName: scope.displayName,
      description: scope.description,
      enabled: scope.enabled,
    });
    setEditingScopeKey(clientKey);
    setScopeErrors({});
  };

  const removeScope = (clientKey) => {
    setScopes((prev) => prev.filter((scope) => scope.clientKey !== clientKey));

    if (editingScopeKey === clientKey) {
      resetScopeDraft();
    }
  };

  const onSubmit = async (data) => {
    const payload = {
      id: data.id || id || "00000000-0000-0000-0000-000000000000",
      name: data.name.trim(),
      displayName: data.displayName.trim(),
      description: data.description?.trim() || null,
      enabled: !!data.enabled,
      scopes: scopes
        .filter((scope) => scope.name.trim() || scope.displayName.trim())
        .map((scope) => ({
          id: scope.id,
          name: scope.name.trim(),
          displayName: scope.displayName.trim(),
          description: scope.description?.trim() || null,
          enabled: !!scope.enabled,
        })),
    };

    const result =
      mode === "edit" && id
        ? await updateApiResource(id, payload)
        : await createApiResource(payload);

    if (!result.ok) {
      return;
    }

    setSuccess({
      title: mode === "edit" ? "ApiResource updated" : "ApiResource created",
      message:
        mode === "edit"
          ? "ApiResource updated successfully."
          : "ApiResource created successfully.",
    });
    navigate("/api-resources");
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">
            {mode === "edit" ? "Edit ApiResource" : "Add ApiResource"}
          </h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface form-surface">
        <div className="card form-section-card mb-3">
          <div className="card-body">
            <h6 className="card-title">ApiResource Details</h6>
            <form onSubmit={handleSubmit(onSubmit)}>
              <div className="row g-3">
                <div className="col-12 col-md-6">
                  <label className="form-label">Name *</label>
                  <input
                    className={`form-control${errors.name ? " is-invalid" : ""}`}
                    placeholder="inventory-api"
                    {...register("name", { required: "Name is required." })}
                  />
                  {errors.name && <div className="error-msg">{errors.name.message}</div>}
                </div>
                <div className="col-12 col-md-6">
                  <label className="form-label">Display Name *</label>
                  <input
                    className={`form-control${errors.displayName ? " is-invalid" : ""}`}
                    placeholder="Inventory API"
                    {...register("displayName", { required: "Display name is required." })}
                  />
                  {errors.displayName && (
                    <div className="error-msg">{errors.displayName.message}</div>
                  )}
                </div>
                <div className="col-12">
                  <label className="form-label">Description</label>
                  <textarea className="form-control" rows="3" {...register("description")} />
                </div>
                <div className="col-12">
                  <div className="form-check form-switch app-switch account-status-switch">
                    <input
                      className="form-check-input app-switch-input"
                      type="checkbox"
                      {...register("enabled")}
                    />
                    <label className="form-check-label">Enabled</label>
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>

        <div className="card form-section-card">
          <div className="card-body">
            <h6 className="card-title mb-3">Scopes</h6>
            <div className="row g-3">
              <div className="col-12 col-xl-5">
                <div className="api-resource-scope-panel">
                  <div className="row g-3">
                    <div className="col-12">
                      <label className="form-label">Scope Name *</label>
                      <input
                        className={`form-control${scopeErrors.name ? " is-invalid" : ""}`}
                        value={scopeDraft.name}
                        onChange={(event) => updateScopeDraft("name", event.target.value)}
                        placeholder="inventory.read"
                      />
                      {scopeErrors.name && <div className="error-msg">{scopeErrors.name}</div>}
                    </div>
                    <div className="col-12">
                      <label className="form-label">Display Name *</label>
                      <input
                        className={`form-control${scopeErrors.displayName ? " is-invalid" : ""}`}
                        value={scopeDraft.displayName}
                        onChange={(event) => updateScopeDraft("displayName", event.target.value)}
                        placeholder="Inventory Read"
                      />
                      {scopeErrors.displayName && (
                        <div className="error-msg">{scopeErrors.displayName}</div>
                      )}
                    </div>
                    <div className="col-12">
                      <label className="form-label">Description</label>
                      <textarea
                        className="form-control"
                        rows="4"
                        value={scopeDraft.description}
                        onChange={(event) => updateScopeDraft("description", event.target.value)}
                        placeholder="Optional scope description"
                      />
                    </div>
                    <div className="col-12">
                      <div className="form-check form-switch app-switch account-status-switch basic-info-active">
                        <input
                          className="form-check-input app-switch-input"
                          type="checkbox"
                          checked={scopeDraft.enabled}
                          onChange={(event) => updateScopeDraft("enabled", event.target.checked)}
                        />
                        <label className="form-check-label">Enabled</label>
                      </div>
                    </div>
                    <div className="col-12 d-flex justify-content-end gap-2">
                      <button type="button" className="btn btn-soft" onClick={resetScopeDraft}>
                        <i className="fa fa-eraser me-1"></i>
                        Clear
                      </button>
                      <button type="button" className="btn btn-primary" onClick={saveScope}>
                        <i className="fa fa-save me-1"></i>
                        Save
                      </button>
                    </div>
                  </div>
                </div>
              </div>
              <div className="col-12 col-xl-7">
                <div className="api-resource-scope-panel api-resource-scope-panel-divider">
                  <div className="table-responsive">
                    <table className="table table-hover align-middle table-striped table-bordered">
                      <thead>
                        <tr>
                          <th>Scope Name</th>
                          <th>Display Name</th>
                          <th>Description</th>
                          <th>Status</th>
                          <th className="text-right">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {scopes.map((scope) => (
                          <tr
                            key={scope.clientKey}
                            className={
                              editingScopeKey === scope.clientKey
                                ? "api-resource-scope-row-active"
                                : ""
                            }
                          >
                            <td>{scope.name}</td>
                            <td>{scope.displayName}</td>
                            <td>{scope.description || "--"}</td>
                            <td>
                              <span
                                className={`status-pill ${
                                  scope.enabled ? "status-pill-success" : "status-pill-off"
                                }`}
                              >
                                {scope.enabled ? "Active" : "Disabled"}
                              </span>
                            </td>
                            <td className="text-right table-actions">
                              <button
                                className="btn btn-link p-0 text-primary ButtonLink"
                                type="button"
                                title="Edit scope"
                                onClick={() => editScope(scope.clientKey)}
                              >
                                <i className="fa fa-pen"></i>
                              </button>
                              <button
                                className="btn btn-link p-0 text-danger ButtonLink"
                                type="button"
                                title="Remove scope"
                                onClick={() => removeScope(scope.clientKey)}
                              >
                                <i className="fa fa-trash"></i>
                              </button>
                            </td>
                          </tr>
                        ))}
                        {scopes.length === 0 && (
                          <tr>
                            <td colSpan="5" className="text-center text-muted py-4">
                              No scopes added for this ApiResource.
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="d-flex justify-content-end gap-2 mt-4">
        <button type="button" className="btn btn-soft" onClick={() => navigate(-1)}>
          <i className="fa fa-times me-1"></i>
          Cancel
        </button>
        <button className="btn btn-primary" onClick={handleSubmit(onSubmit)} disabled={state.loading}>
          <i className="fa fa-save me-1"></i>
          {mode === "edit" ? "Update ApiResource" : "Save ApiResource"}
        </button>
      </div>
    </div>
  );
}

export default AddEditApiResource;
