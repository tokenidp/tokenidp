import React, { useEffect } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import { useTenants } from "../../_hooks/useTenants";

const defaultValues = {
  tenantName: "",
  tenantCode: "",
  email: "",
  isActive: "true",
  tenantType: "0",
  subscriptionType: "0",
  authenticationMode: "0",
  theme: "",
  logoUrl: "",
  primaryColor: "",
  defaultLanguage: "",
  loginText: "",
  twoFactorEnabled: false,
  twoFactorCodeExpiry: 300,
  homePageUrl: "",
};

function AddEditTenant({ mode }) {
  const navigate = useNavigate();
  const { tenantId } = useParams();
  const id = tenantId;
  const {
    state,
    loadLookups,
    getTenantById,
    createTenant,
    updateTenant,
    clearStatus,
  } = useTenants();

  const {
    register,
    handleSubmit,
    setValue,
    reset,
    watch,
    formState: { errors },
  } = useForm({
    defaultValues,
  });

  const isActiveValue = watch("isActive");
  const twoFactorEnabled = watch("twoFactorEnabled");
  const isActive = String(isActiveValue).toLowerCase() === "true";

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    if (mode !== "edit" || !id) {
      return;
    }

    const loadTenant = async () => {
      const data = await getTenantById(id);
      if (!data) {
        return;
      }

      reset({
        tenantName: data.tenantName ?? data.TenantName ?? "",
        tenantCode: data.tenantCode ?? data.TenantCode ?? "",
        email: data.email ?? data.Email ?? "",
        isActive: String(data.isActive ?? data.IsActive ?? true).toLowerCase(),
        tenantType: String(data.tenantType ?? data.TenantType ?? 0),
        subscriptionType: String(data.subscriptionType ?? data.SubscriptionType ?? 0),
        authenticationMode: String(data.authenticationMode ?? data.AuthenticationMode ?? 0),
        theme: data.theme ?? data.Theme ?? "",
        logoUrl: data.logoUrl ?? data.LogoUrl ?? "",
        primaryColor: data.primaryColor ?? data.PrimaryColor ?? "",
        defaultLanguage: data.defaultLanguage ?? data.DefaultLanguage ?? "",
        loginText: data.loginText ?? data.LoginText ?? "",
        twoFactorEnabled: data.twoFactorEnabled ?? data.TwoFactorEnabled ?? false,
        twoFactorCodeExpiry: data.twoFactorCodeExpiry ?? data.TwoFactorCodeExpiry ?? 300,
        homePageUrl: data.homePageUrl ?? data.HomePageUrl ?? "",
      });
    };

    loadTenant();
  }, [getTenantById, id, mode, reset]);

  const onSubmit = async (data) => {
    const payload = {
      id: mode === "edit" ? Number(id) : 0,
      tenantName: data.tenantName.trim(),
      tenantCode: data.tenantCode.trim() || null,
      email: data.email.trim() || null,
      theme: data.theme || null,
      logoUrl: data.logoUrl.trim() || null,
      primaryColor: data.primaryColor || null,
      defaultLanguage: data.defaultLanguage || null,
      loginText: data.loginText.trim() || null,
      twoFactorEnabled: !!data.twoFactorEnabled,
      twoFactorCodeExpiry: data.twoFactorEnabled
        ? Number(data.twoFactorCodeExpiry)
        : null,
      homePageUrl: data.homePageUrl.trim() || null,
      isActive: String(data.isActive).toLowerCase() === "true",
      tenantType: Number(data.tenantType),
      subscriptionType: Number(data.subscriptionType),
      authenticationMode: Number(data.authenticationMode),
    };

    const result =
      mode === "edit"
        ? await updateTenant(id, payload)
        : await createTenant(payload);

    if (result.ok) {
      clearStatus();
      navigate("/tenants");
    }
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">
            {mode === "add" ? "Create Tenant" : "Edit Tenant"}
          </h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      {!isActive && (
        <div className="alert alert-warning">
          Tenant is inactive. Users will be unable to authenticate.
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="card-surface form-surface">
          <div className="row g-3">
          <div className="col-12">
            <div className="card">
              <div className="card-body">
                <h6 className="card-title">Tenant Identity</h6>
                <div className="row g-3">
                  <div className="col-12 col-md-6">
                    <label className="form-label">Tenant Name *</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-building"></i>
                      </span>
                      <input
                        className={`form-control${errors.tenantName ? " is-invalid" : ""}`}
                        placeholder="Acme Corporation"
                        {...register("tenantName", { required: true })}
                      />
                    </div>
                    {errors.tenantName && (
                      <div className="error-msg">Tenant name is required.</div>
                    )}
                  </div>
                  <div className="col-12 col-md-6">
                    <label className="form-label">Tenant Code</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-fingerprint"></i>
                      </span>
                      <input
                        className="form-control text-uppercase"
                        readOnly={mode === "edit"}
                        {...register("tenantCode")}
                      />
                    </div>
                    <div className="form-text">
                      Tenant code is system-managed after creation.
                    </div>
                  </div>
                  <div className="col-12 col-md-6">
                    <label className="form-label">Primary Email</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-envelope"></i>
                      </span>
                      <input
                        className={`form-control${errors.email ? " is-invalid" : ""}`}
                        type="email"
                        placeholder="admin@acme.com"
                        {...register("email")}
                      />
                    </div>
                    {errors.email && (
                      <div className="error-msg">Enter a valid email.</div>
                    )}
                  </div>
                  <div className="col-12 col-md-6">
                    <label className="form-label">Status</label>
                    <select
                      className="form-select"
                      {...register("isActive", { required: true })}
                    >
                      {state.statuses.map((option) => (
                        <option
                          key={option.key ?? option.id ?? option.Key ?? option.Id}
                          value={option.key ?? option.id ?? option.Key ?? option.Id}
                        >
                          {option.value ?? option.name ?? option.Value ?? option.Name}
                        </option>
                      ))}
                    </select>
                    {errors.isActive && (
                      <div className="error-msg">Status is required.</div>
                    )}
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Tenant Type</label>
                    <select
                      className="form-select"
                      {...register("tenantType", { required: true })}
                    >
                      {state.tenantTypes.map((option) => (
                        <option
                          key={option.key ?? option.id ?? option.Key ?? option.Id}
                          value={option.key ?? option.id ?? option.Key ?? option.Id}
                        >
                          {option.value ?? option.name ?? option.Value ?? option.Name}
                        </option>
                      ))}
                    </select>
                    {errors.tenantType && (
                      <div className="error-msg">Tenant type is required.</div>
                    )}
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Subscription Type</label>
                    <select
                      className="form-select"
                      {...register("subscriptionType", { required: true })}
                    >
                      {state.subscriptionTypes.map((option) => (
                        <option
                          key={option.key ?? option.id ?? option.Key ?? option.Id}
                          value={option.key ?? option.id ?? option.Key ?? option.Id}
                        >
                          {option.value ?? option.name ?? option.Value ?? option.Name}
                        </option>
                      ))}
                    </select>
                    {errors.subscriptionType && (
                      <div className="error-msg">
                        Subscription type is required.
                      </div>
                    )}
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Authentication Mode</label>
                    <select
                      className="form-select"
                      {...register("authenticationMode", { required: true })}
                    >
                      {state.authenticationModes.map((option) => (
                        <option
                          key={option.key ?? option.id ?? option.Key ?? option.Id}
                          value={option.key ?? option.id ?? option.Key ?? option.Id}
                        >
                          {option.value ?? option.name ?? option.Value ?? option.Name}
                        </option>
                      ))}
                    </select>
                    {errors.authenticationMode && (
                      <div className="error-msg">
                        Authentication mode is required.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-12">
            <div className="card">
              <div className="card-body">
                <h6 className="card-title">Branding &amp; UI Customization</h6>
                <div className="text-muted small mb-3">Tenant Login Experience</div>
                <div className="row g-3">
                  <div className="col-12 col-md-4">
                    <label className="form-label">Theme</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-palette"></i>
                      </span>
                      <input className="form-control" {...register("theme")} />
                    </div>
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Primary Color</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-droplet"></i>
                      </span>
                      <input
                        className="form-control form-control-color"
                        type="color"
                        {...register("primaryColor")}
                      />
                    </div>
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Default Language</label>
                    <input
                      className="form-control"
                      placeholder="English"
                      {...register("defaultLanguage")}
                    />
                  </div>
                  <div className="col-12 col-md-6">
                    <label className="form-label">Logo URL</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-image"></i>
                      </span>
                      <input
                        className="form-control"
                        placeholder="https://cdn.acme.com/logo.svg"
                        {...register("logoUrl")}
                      />
                    </div>
                  </div>
                  <div className="col-12 col-md-6">
                    <label className="form-label">Home Page URL</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-house"></i>
                      </span>
                      <input
                        className="form-control"
                        placeholder="https://portal.acme.com"
                        {...register("homePageUrl")}
                      />
                    </div>
                  </div>
                  <div className="col-12">
                    <label className="form-label">Login Text</label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-align-left"></i>
                      </span>
                      <textarea
                        className="form-control"
                        rows="3"
                        placeholder="Welcome to Acme Identity Portal."
                        {...register("loginText")}
                      ></textarea>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-12">
            <div className="card tenant-security-card">
              <div className="card-body">
                <h6 className="card-title">Security Settings</h6>
                <div className="row g-2 align-items-center">
                  <div className="col-12 col-md-6">
                    <label className="form-label">Two-Factor Authentication</label>
                    <div className="form-check form-switch app-switch account-status-switch">
                      <input
                        className="form-check-input app-switch-input"
                        type="checkbox"
                        {...register("twoFactorEnabled")}
                        onChange={(event) =>
                          setValue("twoFactorEnabled", event.target.checked)
                        }
                      />
                      <label className="form-check-label">Enabled</label>
                    </div>
                    <div className="form-text">
                      Enforce MFA for all tenant users.
                    </div>
                  </div>
                  <div className="col-12 col-md-6">
                    <label className="form-label">
                      Two-Factor Code Expiry (sec)
                    </label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-clock"></i>
                      </span>
                      <input
                        className="form-control"
                        type="number"
                        min="30"
                        placeholder="300"
                        disabled={!twoFactorEnabled}
                        {...register("twoFactorCodeExpiry", {
                          validate: (value) =>
                            !twoFactorEnabled || (value && Number(value) >= 30),
                        })}
                      />
                    </div>
                    {!twoFactorEnabled && (
                      <div className="form-text text-muted">
                        Enable MFA to configure expiry.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

          <div className="d-flex justify-content-end gap-2 mt-4">
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={() => navigate("/tenants")}
            >
              Cancel
            </button>
            <button className="btn btn-primary-solid" type="submit" disabled={state.loading}>
              {state.loading ? "Saving..." : "Save Changes"}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}

export default AddEditTenant;
