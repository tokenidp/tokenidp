import React, { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import ProviderIcon from "../common/providerIcon";
import { useTenants } from "../../_hooks/useTenants";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

const PROVIDER_TYPES = [
  "Google",
  "Microsoft",
  "GitHub",
  "Apple",
  "LinkedIn",
  "Okta",
];

const DEFAULT_SEARCH = {
  pageNumber: 1,
  pageSize: 100,
  sortColumn: "TenantName",
  sortOrder: "asc",
  searchAll: false,
};

const SECRET_MASK = "********";
const SECRET_AUTO_HIDE_MS = 6000;

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const getProviderName = (providerType) => {
  if (typeof providerType === "string" && providerType.trim()) {
    return providerType.trim();
  }

  const index = Number(providerType);
  return PROVIDER_TYPES[index] || `Provider ${providerType}`;
};

const createDraft = (provider) => {
  const providerType = Number(
    provider?.providerType ?? provider?.ProviderType ?? 0
  );
  const hasClientSecret = !!(
    provider?.hasClientSecret ?? provider?.HasClientSecret ?? false
  );

  return {
    providerType,
    enabled: !!(provider?.enabled ?? provider?.Enabled ?? false),
    clientId: String(provider?.clientId ?? provider?.ClientId ?? ""),
    scopes: String(provider?.scopes ?? provider?.Scopes ?? ""),
    clientSecret: "",
    hasClientSecret,
    editingSecret: !hasClientSecret,
    clientSecretChanged: false,
    showSecret: false,
    revealedSecret: "",
  };
};

function TenantSocialSignIn() {
  const navigate = useNavigate();
  const location = useLocation();
  const { tenantKey } = useParams();
  const decodedTenantKey = decodeURIComponent(tenantKey || "");
  const [tenantId, setTenantId] = useState(location?.state?.id ?? null);
  const [tenantSummary, setTenantSummary] = useState(null);
  const [providers, setProviders] = useState([]);
  const [drafts, setDrafts] = useState({});
  const [activeProviderType, setActiveProviderType] = useState(null);
  const [savingProviderType, setSavingProviderType] = useState(null);
  const [providerErrors, setProviderErrors] = useState({});
  const [loadingSocialSignIn, setLoadingSocialSignIn] = useState(false);
  const secretRevealTimeoutsRef = useRef({});
  const { setSuccess } = useGlobalSuccess();
  const {
    state,
    loadTenants,
    resolveTenantIdByCode,
    getTenantSocialSignIn,
    updateTenantSocialProvider,
    revealTenantSocialProviderSecret,
    clearStatus,
  } = useTenants();

  const selectedTenantOption = useMemo(
    () =>
      state.items.find(
        (item) => Number(getField(item, "id", "Id")) === Number(tenantId)
      ) || null,
    [state.items, tenantId]
  );

  const activeDraft =
    activeProviderType !== null ? drafts[activeProviderType] || null : null;

  useEffect(() => {
    loadTenants(DEFAULT_SEARCH);
  }, [loadTenants]);

  useEffect(() => {
    return () => {
      Object.values(secretRevealTimeoutsRef.current).forEach((timeoutId) => {
        window.clearTimeout(timeoutId);
      });
      secretRevealTimeoutsRef.current = {};
    };
  }, []);

  useEffect(() => {
    if (!decodedTenantKey || tenantId) {
      return;
    }

    let ignore = false;

    const resolveTenant = async () => {
      const resolvedId = await resolveTenantIdByCode(decodedTenantKey);
      if (!ignore && resolvedId) {
        setTenantId(Number(resolvedId));
      }
    };

    resolveTenant();
    return () => {
      ignore = true;
    };
  }, [decodedTenantKey, resolveTenantIdByCode, tenantId]);

  useEffect(() => {
    if (tenantId || decodedTenantKey || state.items.length !== 1) {
      return;
    }

    const singleTenantId = Number(getField(state.items[0], "id", "Id"));
    if (singleTenantId > 0) {
      setTenantId(singleTenantId);
    }
  }, [decodedTenantKey, state.items, tenantId]);

  useEffect(() => {
    if (!tenantId) {
      setTenantSummary(null);
      setProviders([]);
      setDrafts({});
      setActiveProviderType(null);
      return;
    }

    let ignore = false;

    const loadSocialSignIn = async () => {
      setLoadingSocialSignIn(true);
      const response = await getTenantSocialSignIn(tenantId);
      if (ignore || !response) {
        setLoadingSocialSignIn(false);
        return;
      }

      const responseProviders = response.providers ?? response.Providers ?? [];
      setTenantSummary({
        tenantId: response.tenantId ?? response.TenantId ?? tenantId,
        tenantName: response.tenantName ?? response.TenantName ?? "",
        tenantCode: response.tenantCode ?? response.TenantCode ?? "",
      });
      setProviders(responseProviders);
      setDrafts(
        responseProviders.reduce((accumulator, provider) => {
          const providerType = Number(
            provider?.providerType ?? provider?.ProviderType ?? 0
          );
          accumulator[providerType] = createDraft(provider);
          return accumulator;
        }, {})
      );
      setProviderErrors({});
      setLoadingSocialSignIn(false);
    };

    loadSocialSignIn();
    return () => {
      ignore = true;
    };
  }, [getTenantSocialSignIn, tenantId]);

  const navigateToTenant = (nextTenantId) => {
    const resolved = state.items.find(
      (item) => Number(getField(item, "id", "Id")) === Number(nextTenantId)
    );
    const nextTenantCode = String(
      getField(resolved, "tenantCode", "TenantCode") || ""
    );

    if (nextTenantCode) {
      navigate(`/tenants/social-sign-in/${encodeURIComponent(nextTenantCode)}`, {
        state: { id: Number(nextTenantId) },
      });
    } else {
      navigate("/tenants/social-sign-in", {
        state: { id: Number(nextTenantId) },
      });
    }
    setTenantId(Number(nextTenantId));
  };

  const updateDraft = (providerType, patch) => {
    setDrafts((prev) => ({
      ...prev,
      [providerType]: {
        ...(prev[providerType] || {}),
        ...patch,
      },
    }));
  };

  const clearProviderError = (providerType, fieldName) => {
    setProviderErrors((prev) => {
      if (!prev[providerType] || !prev[providerType][fieldName]) {
        return prev;
      }

      const nextProviderErrors = { ...prev[providerType] };
      delete nextProviderErrors[fieldName];
      const next = { ...prev };

      if (Object.keys(nextProviderErrors).length) {
        next[providerType] = nextProviderErrors;
      } else {
        delete next[providerType];
      }

      return next;
    });
  };

  const clearRevealTimer = (providerType) => {
    const timeoutId = secretRevealTimeoutsRef.current[providerType];
    if (timeoutId) {
      window.clearTimeout(timeoutId);
      delete secretRevealTimeoutsRef.current[providerType];
    }
  };

  const scheduleSecretAutoHide = (providerType) => {
    clearRevealTimer(providerType);
    secretRevealTimeoutsRef.current[providerType] = window.setTimeout(() => {
      updateDraft(providerType, {
        showSecret: false,
        revealedSecret: "",
      });
      delete secretRevealTimeoutsRef.current[providerType];
    }, SECRET_AUTO_HIDE_MS);
  };

  const openProvider = (providerType) => {
    setActiveProviderType(providerType);
  };

  const closeProvider = () => {
    if (activeProviderType !== null) {
      clearRevealTimer(activeProviderType);
    }
    setActiveProviderType(null);
  };

  const toggleRevealSecret = async (providerType) => {
    if (!tenantId) {
      return;
    }

    const detail = drafts[providerType];
    if (!detail) {
      return;
    }

    if (detail.showSecret) {
      clearRevealTimer(providerType);
      updateDraft(providerType, {
        showSecret: false,
        revealedSecret: "",
      });
      return;
    }

    const providerName = getProviderName(providerType);
    const response = await revealTenantSocialProviderSecret(tenantId, providerName);
    const clientSecret = String(
      response?.clientSecret ?? response?.ClientSecret ?? ""
    );

    if (!clientSecret) {
      return;
    }

    updateDraft(providerType, {
      showSecret: true,
      revealedSecret: clientSecret,
    });
    scheduleSecretAutoHide(providerType);
  };

  const validateProvider = (providerType, detail) => {
    const nextErrors = {};

    if (detail.enabled) {
      if (!String(detail.clientId || "").trim()) {
        nextErrors.clientId = "Client ID is required when provider is enabled.";
      }

      if (!String(detail.scopes || "").trim()) {
        nextErrors.scopes = "Scopes are required when provider is enabled.";
      }

      const hasSecret =
        detail.hasClientSecret || String(detail.clientSecret || "").trim();
      if (!hasSecret) {
        nextErrors.clientSecret =
          "Client secret is required when provider is enabled.";
      }
    }

    setProviderErrors((prev) => {
      const next = { ...prev };
      if (Object.keys(nextErrors).length) {
        next[providerType] = nextErrors;
      } else {
        delete next[providerType];
      }
      return next;
    });

    return Object.keys(nextErrors).length === 0;
  };

  const handleSaveProvider = async () => {
    if (!tenantId || activeProviderType === null || !activeDraft) {
      return;
    }

    if (!validateProvider(activeProviderType, activeDraft)) {
      return;
    }

    setSavingProviderType(activeProviderType);
    const providerName = getProviderName(activeProviderType);
    const payload = {
      enabled: !!activeDraft.enabled,
      clientId: String(activeDraft.clientId || "").trim(),
      scopes: String(activeDraft.scopes || "").trim(),
    };

    if (
      activeDraft.clientSecretChanged &&
      String(activeDraft.clientSecret || "").trim()
    ) {
      payload.clientSecret = String(activeDraft.clientSecret).trim();
    }

    const result = await updateTenantSocialProvider(
      tenantId,
      providerName,
      payload
    );

    setSavingProviderType(null);

    if (!result) {
      return;
    }

    const refreshed = await getTenantSocialSignIn(tenantId);
    if (refreshed) {
      const refreshedProviders = refreshed.providers ?? refreshed.Providers ?? [];
      setProviders(refreshedProviders);
      setDrafts(
        refreshedProviders.reduce((accumulator, provider) => {
          const providerType = Number(
            provider?.providerType ?? provider?.ProviderType ?? 0
          );
          accumulator[providerType] = createDraft(provider);
          return accumulator;
        }, {})
      );
      setTenantSummary({
        tenantId: refreshed.tenantId ?? refreshed.TenantId ?? tenantId,
        tenantName: refreshed.tenantName ?? refreshed.TenantName ?? "",
        tenantCode: refreshed.tenantCode ?? refreshed.TenantCode ?? "",
      });
    }

    clearStatus();
    setSuccess({
      title: "Social sign-in updated",
      message: `${providerName} configuration saved successfully.`,
    });
    closeProvider();
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Social Sign In</h5>
          <Breadcrumbs
            className="app-breadcrumb mb-0"
            appendLabel={tenantSummary?.tenantName || ""}
          />
        </div>
      </div>

      <div className="card-surface form-surface">
        <div className="social-signin-toolbar">
          <div>
            <h5 className="mb-1">Tenant Social Sign In</h5>
            <div className="text-muted small">
              Configure external identity providers for the selected tenant.
            </div>
          </div>
          <div className="social-signin-tenant-picker">
            <label className="form-label mb-1">Tenant</label>
            <select
              className="form-select"
              value={tenantId || ""}
              onChange={(event) => {
                const nextTenantId = Number(event.target.value || 0);
                if (nextTenantId > 0) {
                  navigateToTenant(nextTenantId);
                }
              }}
            >
              <option value="">Select tenant</option>
              {state.items.map((item) => {
                const id = Number(getField(item, "id", "Id"));
                const code = String(getField(item, "tenantCode", "TenantCode") || "");
                const name = String(getField(item, "tenantName", "TenantName") || "");
                return (
                  <option key={id} value={id}>
                    {name} {code ? `(${code})` : ""}
                  </option>
                );
              })}
            </select>
          </div>
        </div>

        {!tenantId ? (
          <div className="social-signin-empty-state">
            Select a tenant to manage social sign-in providers.
          </div>
        ) : loadingSocialSignIn ? (
          <div className="social-signin-empty-state">
            Loading tenant social sign-in providers...
          </div>
        ) : (
          <>
            <div className="social-signin-tenant-summary">
              <div className="social-signin-tenant-summary-label">Tenant</div>
              <div className="social-signin-tenant-summary-value">
                {tenantSummary?.tenantName || getField(selectedTenantOption, "tenantName", "TenantName")}
              </div>
              <div className="social-signin-tenant-summary-meta">
                {tenantSummary?.tenantCode || getField(selectedTenantOption, "tenantCode", "TenantCode")}
              </div>
            </div>

            <div className="social-provider-grid">
              {providers.map((provider) => {
                const providerType = Number(
                  provider?.providerType ?? provider?.ProviderType ?? 0
                );
                const providerName = getProviderName(providerType);
                const detail = drafts[providerType] || createDraft(provider);
                const isConfigured =
                  detail.hasClientSecret ||
                  String(detail.clientId || "").trim().length > 0;

                return (
                  <button
                    type="button"
                    key={providerType}
                    className={`social-provider-tile ${
                      detail.enabled ? "is-enabled" : ""
                    }`}
                    onClick={() => openProvider(providerType)}
                  >
                    <div className="social-provider-tile-top">
                      <span
                        className={`status-pill ${
                          detail.enabled
                            ? "status-pill-success"
                            : isConfigured
                              ? "status-pill-warning"
                              : "status-pill-off"
                        }`}
                      >
                        {detail.enabled
                          ? "Enabled"
                          : isConfigured
                            ? "Configured"
                            : "Not Configured"}
                      </span>
                    </div>
                    <div className="social-provider-tile-icon">
                      <ProviderIcon label={providerName} size={34} />
                    </div>
                    <div className="social-provider-tile-title">{providerName}</div>
                    <div className="social-provider-tile-meta">
                      {detail.scopes
                        ? detail.scopes
                        : "Client ID, secret, and scopes"}
                    </div>
                  </button>
                );
              })}
            </div>
          </>
        )}
      </div>

      {activeDraft && activeProviderType !== null && (
        <>
          <div className="drawer-backdrop" onClick={closeProvider}></div>
          <aside className="side-drawer" aria-modal="true" role="dialog">
            <div className="side-drawer-header">
              <div>
                <div className="side-drawer-title">
                  <ProviderIcon
                    label={getProviderName(activeProviderType)}
                    size={22}
                  />
                  <span>{getProviderName(activeProviderType)}</span>
                </div>
                <div className="text-muted small">
                  Configure provider credentials and requested scopes.
                </div>
              </div>
              <button
                type="button"
                className="btn btn-link"
                onClick={closeProvider}
              >
                <i className="fa fa-times"></i>
              </button>
            </div>

            <div className="side-drawer-body">
              <div className="mb-3">
                <label className="form-label">Status</label>
                <div className="form-check form-switch app-switch account-status-switch">
                  <input
                    className="form-check-input app-switch-input"
                    type="checkbox"
                    checked={!!activeDraft.enabled}
                    onChange={(event) =>
                      updateDraft(activeProviderType, {
                        enabled: event.target.checked,
                      })
                    }
                  />
                  <label className="form-check-label">
                    {activeDraft.enabled ? "Enabled" : "Disabled"}
                  </label>
                </div>
              </div>

              <div className="mb-3">
                <label className="form-label">Client ID</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-fingerprint"></i>
                  </span>
                  <input
                    className={`form-control${
                      providerErrors?.[activeProviderType]?.clientId
                        ? " is-invalid"
                        : ""
                    }`}
                    value={activeDraft.clientId || ""}
                    onChange={(event) => {
                      updateDraft(activeProviderType, {
                        clientId: event.target.value,
                      });
                      clearProviderError(activeProviderType, "clientId");
                    }}
                  />
                </div>
                {providerErrors?.[activeProviderType]?.clientId && (
                  <div className="error-msg">
                    {providerErrors[activeProviderType].clientId}
                  </div>
                )}
              </div>

              <div className="mb-3">
                <label className="form-label">Client Secret</label>
                {activeDraft.hasClientSecret && !activeDraft.editingSecret ? (
                  <div>
                    <input
                      className="form-control mb-2"
                      type={activeDraft.showSecret ? "text" : "password"}
                      readOnly
                      value={
                        activeDraft.showSecret
                          ? activeDraft.revealedSecret || ""
                          : SECRET_MASK
                      }
                    />
                    <div className="d-flex gap-2">
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() => toggleRevealSecret(activeProviderType)}
                      >
                        {activeDraft.showSecret ? "Hide Secret" : "Show Secret"}
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() => {
                          clearRevealTimer(activeProviderType);
                          updateDraft(activeProviderType, {
                            editingSecret: true,
                            showSecret: false,
                            revealedSecret: "",
                          });
                        }}
                      >
                        Change Secret
                      </button>
                    </div>
                  </div>
                ) : (
                  <div>
                    <div className="input-group">
                      <span className="input-group-text">
                        <i className="fa fa-key"></i>
                      </span>
                      <input
                        className={`form-control${
                          providerErrors?.[activeProviderType]?.clientSecret
                            ? " is-invalid"
                            : ""
                        }`}
                        type="password"
                        value={activeDraft.clientSecret || ""}
                        onChange={(event) => {
                          updateDraft(activeProviderType, {
                            clientSecret: event.target.value,
                            clientSecretChanged: Boolean(
                              String(event.target.value || "").trim()
                            ),
                          });
                          clearProviderError(activeProviderType, "clientSecret");
                        }}
                      />
                    </div>
                    {providerErrors?.[activeProviderType]?.clientSecret && (
                      <div className="error-msg">
                        {providerErrors[activeProviderType].clientSecret}
                      </div>
                    )}
                  </div>
                )}
              </div>

              <div className="mb-3">
                <label className="form-label">Scopes</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-layer-group"></i>
                  </span>
                  <textarea
                    className={`form-control${
                      providerErrors?.[activeProviderType]?.scopes
                        ? " is-invalid"
                        : ""
                    }`}
                    rows="4"
                    placeholder="openid profile email"
                    value={activeDraft.scopes || ""}
                    onChange={(event) => {
                      updateDraft(activeProviderType, {
                        scopes: event.target.value,
                      });
                      clearProviderError(activeProviderType, "scopes");
                    }}
                  ></textarea>
                </div>
                {providerErrors?.[activeProviderType]?.scopes && (
                  <div className="error-msg">
                    {providerErrors[activeProviderType].scopes}
                  </div>
                )}
                <div className="form-text mt-2">
                  Separate multiple scopes with spaces, matching the provider
                  requirements.
                </div>
              </div>
            </div>

            <div className="side-drawer-footer">
              <button
                type="button"
                className="btn btn-soft"
                onClick={closeProvider}
              >
                <i className="fa fa-times me-1"></i>
                Cancel
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={handleSaveProvider}
                disabled={savingProviderType === activeProviderType}
              >
                <i className="fa fa-save pe-2"></i>
                {savingProviderType === activeProviderType
                  ? "Saving..."
                  : "Save Provider"}
              </button>
            </div>
          </aside>
        </>
      )}
    </div>
  );
}

export default TenantSocialSignIn;
