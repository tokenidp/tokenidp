import React, { useEffect, useState } from "react";
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

function AddEditApiResource({ mode = "add" }) {
  const navigate = useNavigate();
  const { id } = useParams();
  const { state, getApiResourceById, createApiResource, updateApiResource } = useApiResources();
  const { setSuccess } = useGlobalSuccess();
  const [scopes, setScopes] = useState([emptyScope()]);

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
      setScopes(
        nextScopes.length
          ? nextScopes.map((scope) => ({
              id: scope.id ?? scope.Id ?? null,
              name: scope.name ?? scope.Name ?? "",
              displayName: scope.displayName ?? scope.DisplayName ?? "",
              description: scope.description ?? scope.Description ?? "",
              enabled: scope.enabled ?? scope.Enabled ?? true,
            }))
          : [emptyScope()]
      );
    };

    load();
  }, [getApiResourceById, id, mode, setValue]);

  const updateScope = (index, field, value) => {
    setScopes((prev) =>
      prev.map((scope, currentIndex) =>
        currentIndex === index ? { ...scope, [field]: value } : scope
      )
    );
  };

  const addScope = () => setScopes((prev) => [...prev, emptyScope()]);

  const removeScope = (index) => {
    setScopes((prev) => (prev.length === 1 ? [emptyScope()] : prev.filter((_, i) => i !== index)));
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
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h6 className="card-title mb-0">Scopes</h6>
              <button type="button" className="btn btn-soft" onClick={addScope}>
                <i className="fa fa-plus me-1"></i>
                Add Scope
              </button>
            </div>

            <div className="row g-3">
              {scopes.map((scope, index) => (
                <div className="col-12" key={scope.id || `scope-${index}`}>
                  <div className="card border">
                    <div className="card-body">
                      <div className="row g-3">
                        <div className="col-12 col-md-4">
                          <label className="form-label">Scope Name *</label>
                          <input
                            className="form-control"
                            value={scope.name}
                            onChange={(event) => updateScope(index, "name", event.target.value)}
                            placeholder="inventory.read"
                          />
                        </div>
                        <div className="col-12 col-md-4">
                          <label className="form-label">Display Name *</label>
                          <input
                            className="form-control"
                            value={scope.displayName}
                            onChange={(event) =>
                              updateScope(index, "displayName", event.target.value)
                            }
                            placeholder="Inventory Read"
                          />
                        </div>
                        <div className="col-12 col-md-3">
                          <label className="form-label">Enabled</label>
                          <div className="form-check form-switch mt-2">
                            <input
                              className="form-check-input"
                              type="checkbox"
                              checked={scope.enabled}
                              onChange={(event) =>
                                updateScope(index, "enabled", event.target.checked)
                              }
                            />
                          </div>
                        </div>
                        <div className="col-12 col-md-1 d-flex align-items-end justify-content-end">
                          <button
                            type="button"
                            className="btn btn-link text-danger p-0"
                            onClick={() => removeScope(index)}
                            title="Remove scope"
                          >
                            <i className="fa fa-trash"></i>
                          </button>
                        </div>
                        <div className="col-12">
                          <label className="form-label">Description</label>
                          <input
                            className="form-control"
                            value={scope.description}
                            onChange={(event) =>
                              updateScope(index, "description", event.target.value)
                            }
                            placeholder="Optional scope description"
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
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
