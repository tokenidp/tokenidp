import React, { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import { useTenants } from "../../_hooks/useTenants";

const defaultValues = {
  tenantName: "",
  tenantCode: "",
  email: "",
  isActive: "true",
  authenticationMode: "0",
  allowLocalLogin: true,
  requireEmailVerification: false,
  allowSelfRegistration: false,
  theme: "",
  logoUrl: "",
  primaryColor: "",
  loginText: "",
  twoFactorEnabled: false,
  twoFactorCodeExpiry: 5,
  externalProviderKeys: [],
  providers: [],
};

const isProviderEnabled = (provider) =>
  provider?.enabled ?? provider?.Enabled ?? false;

function AddEditTenant({ mode }) {
  const navigate = useNavigate();
  const location = useLocation();
  const { tenantKey } = useParams();
  const decodedTenantKey = decodeURIComponent(tenantKey || "");
  const [tenantId, setTenantId] = useState(location?.state?.id ?? null);
  const [existingProviders, setExistingProviders] = useState([]);
  const {
    state,
    loadLookups,
    getTenantById,
    resolveTenantIdByCode,
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
  const allowLocalLogin = watch("allowLocalLogin");
  const requireEmailVerification = watch("requireEmailVerification");
  const allowSelfRegistration = watch("allowSelfRegistration");
  const isActive = String(isActiveValue).toLowerCase() === "true";

  const getLookupField = (option) =>
    option?.key ??
    option?.Key ??
    option?.id ??
    option?.Id ??
    option?.value ??
    option?.Value ??
    option?.name ??
    option?.Name;

  const resolveProviderEnum = (value) => {
    const raw = String(value ?? "").trim();
    if (!raw) return null;

    if (/^\d+$/.test(raw)) {
      return Number(raw);
    }

    const index = (state.externalProviders ?? []).findIndex((option) => {
      const candidate = String(getLookupField(option) ?? "").trim();
      const label = String(
        option?.value ?? option?.Value ?? option?.name ?? option?.Name ?? ""
      ).trim();
      return (
        raw.toLowerCase() === candidate.toLowerCase() ||
        raw.toLowerCase() === label.toLowerCase()
      );
    });

    return index >= 0 ? index : null;
  };

  const toProviderKey = (provider) => {
    const raw =
      provider?.providerType ??
      provider?.ProviderType ??
      provider?.key ??
      provider?.Key ??
      provider?.id ??
      provider?.Id;

    const enumValue = resolveProviderEnum(raw);
    return enumValue !== null ? String(enumValue) : String(raw ?? "");
  };

  const toProviderOptionValue = (option, index) => {
    const raw = getLookupField(option);
    const enumValue = resolveProviderEnum(raw);
    return String(enumValue ?? index);
  };

  const toNumberOrDefault = (value, fallback = 0) => {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
  };

  const createDefaultProvider = (providerType) => ({
    providerType: toNumberOrDefault(providerType),
    enabled: true,
    clientId: `provider-${providerType}-client`,
    clientSecret: null,
    authority: "https://example.com",
    scopes: "openid profile email",
    callbackPath: "/signin-oidc",
  });

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    if (mode !== "edit") {
      return;
    }

    const loadTenant = async () => {
      let resolvedId = tenantId;
      if (!resolvedId && decodedTenantKey) {
        resolvedId = await resolveTenantIdByCode(decodedTenantKey);
        if (resolvedId) {
          setTenantId(resolvedId);
        }
      }

      if (!resolvedId) {
        return;
      }

      const data = await getTenantById(resolvedId);
      if (!data) {
        return;
      }

      reset({
        tenantName: data.tenantName ?? data.TenantName ?? "",
        tenantCode: data.tenantCode ?? data.TenantCode ?? "",
        email: data.email ?? data.Email ?? "",
        isActive: String(data.isActive ?? data.IsActive ?? true).toLowerCase(),
        authenticationMode: String(
          data.authSettings?.authenticationMode ??
            data.authSettings?.AuthenticationMode ??
            data.authenticationMode ??
            data.AuthenticationMode ??
            0
        ),
        allowLocalLogin:
          data.authSettings?.allowLocalLogin ??
          data.authSettings?.AllowLocalLogin ??
          true,
        requireEmailVerification:
          data.authSettings?.requireEmailVerification ??
          data.authSettings?.RequireEmailVerification ??
          false,
        allowSelfRegistration:
          data.authSettings?.allowSelfRegistration ??
          data.authSettings?.AllowSelfRegistration ??
          false,
        theme:
          data.uiSetting?.theme ??
          data.uiSetting?.Theme ??
          data.theme ??
          data.Theme ??
          "",
        logoUrl:
          data.uiSetting?.logoUrl ??
          data.uiSetting?.LogoUrl ??
          data.logoUrl ??
          data.LogoUrl ??
          "",
        primaryColor:
          data.uiSetting?.primaryColor ??
          data.uiSetting?.PrimaryColor ??
          data.primaryColor ??
          data.PrimaryColor ??
          "",
        loginText:
          data.uiSetting?.loginText ??
          data.uiSetting?.LoginText ??
          data.loginText ??
          data.LoginText ??
          "",
        twoFactorEnabled:
          data.authSettings?.twoFactorEnabled ??
          data.authSettings?.TwoFactorEnabled ??
          data.twoFactorEnabled ??
          data.TwoFactorEnabled ??
          false,
        twoFactorCodeExpiry:
          data.authSettings?.twoFactorCodeExpiry ??
          data.authSettings?.TwoFactorCodeExpiry ??
          data.twoFactorCodeExpiry ??
          data.TwoFactorCodeExpiry ??
          5,
        externalProviderKeys: (data.providers ?? data.Providers ?? [])
          .filter((provider) => isProviderEnabled(provider))
          .map((provider) => toProviderKey(provider))
          .filter(Boolean),
        providers: data.providers ?? data.Providers ?? [],
      });
      setExistingProviders(data.providers ?? data.Providers ?? []);
    };

    loadTenant();
  }, [decodedTenantKey, getTenantById, mode, reset, resolveTenantIdByCode, tenantId]);

  useEffect(() => {
    if (!existingProviders.length || !state.externalProviders.length) {
      return;
    }

    setValue(
      "externalProviderKeys",
      existingProviders
        .filter((provider) => isProviderEnabled(provider))
        .map((provider) => toProviderKey(provider))
        .filter(Boolean)
    );
  }, [existingProviders, setValue, state.externalProviders]);

  const onSubmit = async (data) => {
    if (mode === "edit" && !tenantId) {
      return;
    }

    const selectedProviderKeys = new Set(
      (Array.isArray(data.externalProviderKeys) ? data.externalProviderKeys : [])
        .map((key) => String(key))
        .filter(Boolean)
    );

    const existingByKey = new Map(
      (existingProviders ?? [])
        .map((provider) => [toProviderKey(provider), provider])
        .filter(([key]) => !!key)
    );

    const updatedExistingProviders = Array.from(existingByKey.values()).map(
      (provider) => {
        const providerKey = toProviderKey(provider);
        const providerEnum = resolveProviderEnum(
          provider.providerType ?? provider.ProviderType ?? providerKey
        );
        return {
          providerType: toNumberOrDefault(providerEnum ?? providerKey),
          enabled: selectedProviderKeys.has(providerKey),
          clientId: provider.clientId ?? provider.ClientId ?? "",
          clientSecret: provider.clientSecret ?? provider.ClientSecret ?? null,
          authority: provider.authority ?? provider.Authority ?? "",
          scopes: provider.scopes ?? provider.Scopes ?? "",
          callbackPath: provider.callbackPath ?? provider.CallbackPath ?? "",
        };
      }
    );

    const newSelectedProviders = Array.from(selectedProviderKeys)
      .filter((providerKey) => !existingByKey.has(providerKey))
      .map((providerKey) => createDefaultProvider(providerKey));

    const payload = {
      id: mode === "edit" ? Number(tenantId) : 0,
      tenantName: data.tenantName.trim(),
      tenantCode: data.tenantCode.trim() || "",
      email: data.email.trim() || null,
      isActive: String(data.isActive).toLowerCase() === "true",
      authSettings: {
        authenticationMode: Number(data.authenticationMode),
        allowLocalLogin: data.allowLocalLogin ?? true,
        requireEmailVerification: data.requireEmailVerification ?? false,
        allowSelfRegistration: data.allowSelfRegistration ?? false,
        twoFactorEnabled: !!data.twoFactorEnabled,
        twoFactorCodeExpiry: data.twoFactorEnabled
          ? Number(data.twoFactorCodeExpiry)
          : null,
      },
      uiSetting: {
        theme: data.theme || null,
        logoUrl: data.logoUrl.trim() || null,
        primaryColor: data.primaryColor || null,
        loginText: data.loginText.trim() || null,
      },
      providers: [...updatedExistingProviders, ...newSelectedProviders],
    };

    const result =
      mode === "edit" && tenantId
        ? await updateTenant(tenantId, payload)
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
                        disabled
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
                    <div className="form-check form-switch app-switch account-status-switch">
                      <input
                        className="form-check-input app-switch-input"
                        type="checkbox"
                        checked={isActive}
                        onChange={(event) =>
                          setValue(
                            "isActive",
                            event.target.checked ? "true" : "false",
                            { shouldDirty: true }
                          )
                        }
                      />
                      <label className="form-check-label">
                        {isActive ? "Active" : "Inactive"}
                      </label>
                    </div>
                    <input
                      type="hidden"
                      {...register("isActive", { required: true })}
                    />
                    {errors.isActive && (
                      <div className="error-msg">Status is required.</div>
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
                      <select className="form-select" {...register("theme")}>
                        <option value="">Select Theme</option>
                        {state.themes.map((option) => (
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
                <div className="row g-3 align-items-center">
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
                  <div className="col-12 col-md-4">
                    <label className="form-label">Allow Local Login</label>
                    <div className="form-check form-switch app-switch account-status-switch">
                      <input
                        className="form-check-input app-switch-input"
                        type="checkbox"
                        {...register("allowLocalLogin")}
                      />
                      <label className="form-check-label">
                        {allowLocalLogin ? "Enabled" : "Disabled"}
                      </label>
                    </div>
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Require Email Verification</label>
                    <div className="form-check form-switch app-switch account-status-switch">
                      <input
                        className="form-check-input app-switch-input"
                        type="checkbox"
                        {...register("requireEmailVerification")}
                      />
                      <label className="form-check-label">
                        {requireEmailVerification ? "Enabled" : "Disabled"}
                      </label>
                    </div>
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">Allow Self Registration</label>
                    <div className="form-check form-switch app-switch account-status-switch">
                      <input
                        className="form-check-input app-switch-input"
                        type="checkbox"
                        {...register("allowSelfRegistration")}
                      />
                      <label className="form-check-label">
                        {allowSelfRegistration ? "Enabled" : "Disabled"}
                      </label>
                    </div>
                  </div>
                  <div className="col-12 col-md-4">
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
                      <label className="form-check-label">
                        {twoFactorEnabled ? "Enabled" : "Disabled"}
                      </label>
                    </div>
                    <div className="form-text">
                      Enforce MFA for all tenant users.
                    </div>
                  </div>
                  <div className="col-12 col-md-4">
                    <label className="form-label">
                      Two-Factor Code Expiry (minutes)
                    </label>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-clock"></i>
                      </span>
                      <input
                        className="form-control"
                        type="number"
                        min="1"
                        placeholder="5"
                        disabled={!twoFactorEnabled}
                        {...register("twoFactorCodeExpiry", {
                          validate: (value) =>
                            !twoFactorEnabled || (value && Number(value) >= 1),
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

          <div className="col-12">
            <div className="card">
              <div className="card-body">
                <h6 className="card-title">External Providers</h6>
                <div className="text-muted small mb-3">
                  Select external identity providers from tenant lookups.
                </div>
                <div className="row g-2">
                  {state.externalProviders.map((option, index) => {
                    const value = toProviderOptionValue(option, index);
                    const label =
                      option.value ?? option.name ?? option.Value ?? option.Name;

                    return (
                      <div
                        className="col-12 col-sm-6 col-lg-4"
                        key={String(value)}
                      >
                        <div className="form-check">
                          <input
                            className="form-check-input"
                            type="checkbox"
                            value={String(value)}
                            id={`provider-${String(value)}`}
                            {...register("externalProviderKeys")}
                          />
                          <label
                            className="form-check-label"
                            htmlFor={`provider-${String(value)}`}
                          >
                            {label}
                          </label>
                        </div>
                      </div>
                    );
                  })}
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
