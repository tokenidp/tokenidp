import { TokenIdpAuthConfig, TokenIdpLoginOptions, TokenIdpTenantPropagationMode } from './models';

export function normalizeTenantPropagationMode(value: unknown): TokenIdpTenantPropagationMode {
  const normalized = String(value || 'all').trim().toLowerCase();

  if (normalized === 'none' || normalized === 'api') {
    return normalized;
  }

  return 'all';
}

export function getAuthTenantKey(config: TokenIdpAuthConfig): string {
  return normalizeTenantPropagationMode(config.tenantPropagationMode) === 'all'
    ? String(config.tenantKey || '').trim()
    : '';
}

export function getApiTenantKey(config: TokenIdpAuthConfig): string {
  const mode = normalizeTenantPropagationMode(config.tenantPropagationMode);

  return mode === 'all' || mode === 'api' ? String(config.tenantKey || '').trim() : '';
}

export function resolveRawTenantKey(
  config: TokenIdpAuthConfig,
  overrides: TokenIdpLoginOptions = {},
  locationSearch = '',
): string {
  const explicitTenantKey = String(overrides.tenantKey || config.tenantKey || '').trim();
  if (explicitTenantKey) {
    return explicitTenantKey;
  }

  const tenantFromQuery = new URLSearchParams(locationSearch).get(
    config.tenantQueryParameter || 'tenant',
  );
  if (tenantFromQuery) {
    return tenantFromQuery.trim();
  }

  const tenantFromStorage = globalThis.sessionStorage?.getItem(
    config.tenantKeyStorageKey || 'idp_tenant_key',
  );
  if (tenantFromStorage) {
    return tenantFromStorage.trim();
  }

  return '';
}

export function resolveAuthTenantKey(
  config: TokenIdpAuthConfig,
  overrides: TokenIdpLoginOptions = {},
  locationSearch = '',
): string {
  return normalizeTenantPropagationMode(config.tenantPropagationMode) === 'all'
    ? resolveRawTenantKey(config, overrides, locationSearch)
    : '';
}

export function resolveApiTenantKey(
  config: TokenIdpAuthConfig,
  overrides: TokenIdpLoginOptions = {},
  locationSearch = '',
): string {
  const mode = normalizeTenantPropagationMode(config.tenantPropagationMode);

  return mode === 'all' || mode === 'api' ? resolveRawTenantKey(config, overrides, locationSearch) : '';
}
