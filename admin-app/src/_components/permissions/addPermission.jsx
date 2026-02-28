import React, { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import { usePermissions } from "../../_hooks/usePermissions";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

const keyPattern = /^[a-z0-9]+([._][a-z0-9]+)*$/;

function AddPermission({ mode = "add" }) {
  const navigate = useNavigate();
  const location = useLocation();
  const params = useParams();
  const permissionKeyParam = params.permissionKey;
  const decodedPermissionKey = decodeURIComponent(permissionKeyParam || "");
  const {
    state,
    loadParents,
    createPermission,
    updatePermission,
    getPermissionById,
    resolvePermissionIdByKey,
  } = usePermissions();
  const [permissionId, setPermissionId] = useState(location?.state?.id || null);
  const { setSuccess } = useGlobalSuccess();
  const controlTypeOptions = useMemo(
    () =>
      state.controlTypes.length > 0
        ? state.controlTypes
        : [
            { key: "NavGroup", value: "NavGroup" },
            { key: "NavLink", value: "NavLink" },
            { key: "Action", value: "Action" },
            { key: "WorkflowAction", value: "WorkflowAction" },
          ],
    [state.controlTypes]
  );

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm({
    defaultValues: {
      permissionName: "",
      permissionKey: "",
      parentId: "",
      accessUrl: "",
      icon: "",
      controlType: "NavLink",
      isActive: true,
    },
  });

  const controlType = watch("controlType");
  const isNavControl =
    controlType === "NavGroup" || controlType === "NavLink";

  useEffect(() => {
    loadParents();
  }, [loadParents]);

  useEffect(() => {
    if (mode !== "edit") return;
    const loadPermission = async () => {
      let resolvedId = permissionId;
      if (!resolvedId && decodedPermissionKey) {
        resolvedId = await resolvePermissionIdByKey(decodedPermissionKey);
        if (resolvedId) {
          setPermissionId(resolvedId);
        }
      }

      if (!resolvedId) return;

      const data = await getPermissionById(resolvedId);
      if (!data) return;
      setValue("permissionName", data.permissionName ?? "");
      setValue("permissionKey", data.permissionKey ?? "");
      setValue("parentId", data.parentId ? String(data.parentId) : "");
      setValue("accessUrl", data.accessUrl ?? "");
      setValue("icon", data.icon ?? "");
      setValue("controlType", data.controlType ?? "NavLink");
      setValue("isActive", data.active === "Active");
    };
    loadPermission();
  }, [
    decodedPermissionKey,
    getPermissionById,
    mode,
    permissionId,
    resolvePermissionIdByKey,
    setValue,
  ]);

  const onSubmit = async (data) => {
    const payload = {
      id: permissionId ? Number(permissionId) : 0,
      parentId: data.parentId ? Number(data.parentId) : null,
      permissionKey: data.permissionKey.trim().toLowerCase(),
      permissionName: data.permissionName.trim(),
      accessUrl: data.accessUrl.trim() || null,
      icon: data.icon.trim() || null,
      controlType: data.controlType,
      isActive: !!data.isActive,
    };

    if (mode === "edit" && permissionId) {
      const result = await updatePermission(permissionId, payload);
      if (!result) {
        return;
      }
      setSuccess({
        title: "Permission updated",
        message: "Permission updated successfully.",
      });
    } else {
      const result = await createPermission(payload);
      if (!result) {
        return;
      }
      setSuccess({
        title: "Permission saved",
        message: "Permission created successfully.",
      });
    }
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">
            {mode === "edit" ? "Edit Permission" : "Add Permission"}
          </h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface form-surface">
        <div className="card mb-3">
          <div className="card-body">
            <h6 className="card-title">Control Types</h6>
            <div className="row g-3">
              {controlTypeOptions.map((option) => (
                <div key={option.key || option.value} className="col-12 col-md-6">
                  <div
                    className={`option-card d-flex align-items-center gap-3 ${
                      controlType === (option.key || option.value)
                        ? "option-card-active"
                        : ""
                    }`}
                  >
                    <input
                      className="form-check-input mt-0"
                      type="radio"
                      id={`control-type-${option.key || option.value}`}
                      value={option.key || option.value}
                      {...register("controlType")}
                    />
                    <label
                      className="form-check-label w-100"
                      htmlFor={`control-type-${option.key || option.value}`}
                    >
                      {option.value}
                    </label>
                  </div>
                </div>
              ))}
            </div>
            <div className="form-text mt-2">
              Pick the control type that matches how the permission is used.
            </div>
          </div>
        </div>
        <div className="card">
          <div className="card-body">
            <h6 className="card-title">Permission Details</h6>
            <form onSubmit={handleSubmit(onSubmit)}>
              <div className="row g-3">
                <div className="col-12 col-md-6">
                  <label className="form-label">Permission Name *</label>
                  <input
                    className={`form-control${
                      errors.permissionName ? " is-invalid" : ""
                    }`}
                    placeholder="Manage Users"
                    {...register("permissionName", {
                      required: "Permission name is required.",
                    })}
                  />
                  {errors.permissionName && (
                    <div className="error-msg">
                      {errors.permissionName.message}
                    </div>
                  )}
                </div>

                <div className="col-12 col-md-6">
                  <label className="form-label">Permission Key *</label>
                  <input
                    className={`form-control${
                      errors.permissionKey ? " is-invalid" : ""
                    }`}
                    placeholder="users_manage or users.manage"
                    {...register("permissionKey", {
                      required: "Permission key is required.",
                      pattern: {
                        value: keyPattern,
                        message:
                          "Use lowercase letters, numbers, underscores or dots.",
                      },
                      onChange: (event) => {
                        setValue(
                          "permissionKey",
                          event.target.value.toLowerCase(),
                          {
                            shouldValidate: true,
                            shouldDirty: true,
                          }
                        );
                      },
                    })}
                  />
                  {errors.permissionKey ? (
                    <div className="error-msg">
                      {errors.permissionKey.message}
                    </div>
                  ) : (
                    <div className="text-muted small mt-1">
                      Use lowercase keys (ex: users_manage or users.manage).
                    </div>
                  )}
                </div>

                <div className="col-12 col-md-6">
                  <label className="form-label">Root Menu</label>
                  <select
                    className={`form-select${
                      errors.parentId ? " is-invalid" : ""
                    }`}
                    {...register("parentId", {
                      validate: (value) => {
                        if (
                          (controlType === "Action" ||
                            controlType === "WorkflowAction") &&
                          !value
                        ) {
                          return "Root menu is required for actions.";
                        }
                        return true;
                      },
                    })}
                  >
                    <option value="">Root Menu</option>
                    {state.parents.map((parent) => (
                      <option
                        key={parent.key ?? parent.id ?? parent.Id}
                        value={parent.key ?? parent.id ?? parent.Id}
                      >
                        {parent.value ??
                          parent.permissionName ??
                          parent.PermissionName}
                      </option>
                    ))}
                  </select>
                  {errors.parentId && (
                    <div className="error-msg">{errors.parentId.message}</div>
                  )}
                  <div className="text-muted small mt-1">
                    Actions must be assigned under a menu permission.
                  </div>
                </div>

                <div className="col-12 col-md-6">
                  <label className="form-label">Access URL *</label>
                  <input
                    className="form-control"
                    placeholder="/roles"
                    {...register("accessUrl", {
                      validate: (value) => {
                        if (value && !value.startsWith("/")) {
                          return "Access URL must start with '/'.";
                        }
                        return true;
                      },
                    })}
                  />
                  {errors.accessUrl ? (
                    <div className="error-msg">{errors.accessUrl.message}</div>
                  ) : (
                    <div className="text-muted small mt-1">
                      Example: /roles or /permissions.
                    </div>
                  )}
                </div>

                <div className="col-12 col-md-6">
                  <label className="form-label">Icon (Optional)</label>
                  <input
                    className="form-control"
                    placeholder="fa fa-users"
                    {...register("icon")}
                  />
                </div>

                <div className="col-12 col-md-6">
                  <label className="form-label">Is Active</label>
                  <div className="form-check form-switch app-switch account-status-switch">
                    <input
                      className="form-check-input app-switch-input"
                      type="checkbox"
                      {...register("isActive")}
                    />
                    <label className="form-check-label">Enabled</label>
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div className="d-flex justify-content-end gap-2 mt-4">
        <button
          type="button"
          className="btn btn-outline-secondary"
          onClick={() => navigate(-1)}
        >
          Cancel
        </button>
        <button className="btn btn-primary-solid" onClick={handleSubmit(onSubmit)}>
          {mode === "edit" ? "Update Permission" : "Save Permission"}
        </button>
      </div>
    </div>
  );
}

export default AddPermission;
