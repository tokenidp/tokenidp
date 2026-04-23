export function normalizeTenantPropagationMode(value) {
  const normalized = String(value || "all").trim().toLowerCase();

  if (normalized === "none" || normalized === "api") {
    return normalized;
  }

  return "all";
}

export function getAuthTenantKey(config) {
  return normalizeTenantPropagationMode(config?.tenantPropagationMode) === "all"
    ? String(config?.tenantKey || "").trim()
    : "";
}

export function getApiTenantKey(config) {
  const mode = normalizeTenantPropagationMode(config?.tenantPropagationMode);

  return mode === "all" || mode === "api"
    ? String(config?.tenantKey || "").trim()
    : "";
}

export function resolveRawTenantKey(config, overrides = {}) {
  const explicitTenantKey = String(
    overrides?.tenantKey || config?.tenantKey || "",
  ).trim();
  if (explicitTenantKey) {
    return explicitTenantKey;
  }

  if (typeof window !== "undefined") {
    const tenantFromQuery = new URLSearchParams(window.location.search).get(
      config?.tenantQueryParameter || "tenant",
    );
    if (tenantFromQuery) {
      return tenantFromQuery.trim();
    }
  }

  if (typeof sessionStorage !== "undefined") {
    const tenantFromStorage = sessionStorage.getItem(
      config?.tenantKeyStorageKey || "idp_tenant_key",
    );
    if (tenantFromStorage) {
      return tenantFromStorage.trim();
    }
  }

  return "";
}

export function resolveAuthTenantKey(config, overrides = {}) {
  return normalizeTenantPropagationMode(config?.tenantPropagationMode) === "all"
    ? resolveRawTenantKey(config, overrides)
    : "";
}

export function resolveApiTenantKey(config, overrides = {}) {
  const mode = normalizeTenantPropagationMode(config?.tenantPropagationMode);

  return mode === "all" || mode === "api"
    ? resolveRawTenantKey(config, overrides)
    : "";
}
