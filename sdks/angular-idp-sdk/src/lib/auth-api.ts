import { TokenIdpAuthConfig, TokenIdpTokenParts } from './models';
import { getApiTenantKey, getAuthTenantKey } from './tenant';

interface TokenEndpointPayload {
  grantType: 'authorization_code' | 'refresh_token';
  clientId: string;
  redirectUri?: string;
  code?: string;
  codeVerifier?: string;
  refreshToken?: string;
  scope?: string;
}

async function httpPostJson(url: string, body: unknown): Promise<unknown> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  const data = await readResponseBody(res);

  if (!res.ok) {
    throw buildHttpError(data, res.status);
  }

  return data;
}

async function readResponseBody(res: Response): Promise<unknown> {
  const text = await res.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text) as unknown;
  } catch {
    return text;
  }
}

function buildHttpError(data: unknown, status: number): Error {
  const err = new Error(getErrorMessage(data, status)) as Error & {
    status?: number;
    data?: unknown;
  };
  err.status = status;
  err.data = data;
  return err;
}

function getErrorMessage(data: unknown, status: number): string {
  if (!data || typeof data !== 'object') {
    return `HTTP ${status}`;
  }

  const record = data as Record<string, unknown>;
  const value = record['value'] as Record<string, unknown> | undefined;
  const directError = record['error'];
  const wrappedError =
    directError && typeof directError === 'object'
      ? (directError as Record<string, unknown>)
      : value?.['error'] && typeof value['error'] === 'object'
        ? (value['error'] as Record<string, unknown>)
        : null;

  return (
    stringValue(record['error_description']) ||
    stringValue(wrappedError?.['error']) ||
    stringValue(wrappedError?.['Error']) ||
    stringValue(wrappedError?.['message']) ||
    stringValue(wrappedError?.['Message']) ||
    stringValue(directError) ||
    stringValue(record['message']) ||
    `HTTP ${status}`
  );
}

function stringValue(value: unknown): string {
  return typeof value === 'string' ? value : '';
}

function withTenant(url: string, config: TokenIdpAuthConfig, target: 'api' | 'auth' = 'api'): string {
  const tenantKey = target === 'auth' ? getAuthTenantKey(config) : getApiTenantKey(config);
  if (!tenantKey) {
    return url;
  }

  const tenantUrl = new URL(url);
  tenantUrl.searchParams.set(config.tenantQueryParameter || 'tenant', tenantKey);
  return tenantUrl.toString();
}

export function extractToken(tokenPayload: unknown): TokenIdpTokenParts {
  if (!tokenPayload || typeof tokenPayload !== 'object') {
    return { accessToken: '', refreshToken: '', expiresIn: 0, idToken: '' };
  }

  const record = tokenPayload as Record<string, unknown>;
  const value = ((record['value'] as Record<string, unknown> | undefined) || record) as Record<
    string,
    unknown
  >;

  const accessToken = stringValue(value['accessToken']) || stringValue(value['access_token']);
  const refreshToken = stringValue(value['refreshToken']) || stringValue(value['refresh_token']);
  const expiresIn = Number(value['expiresIn'] || value['expires_in'] || 0) || 0;
  const idToken =
    stringValue(value['idToken']) ||
    stringValue(value['id_token']) ||
    stringValue(record['idToken']) ||
    stringValue(record['id_token']);

  return { accessToken, refreshToken, expiresIn, idToken };
}

export async function exchangeAuthorizationCode(
  config: TokenIdpAuthConfig,
  payload: TokenEndpointPayload,
): Promise<unknown> {
  const url = withTenant(config.authority + (config.tokenPath || '/token'), config, 'auth');
  return httpPostJson(url, payload);
}

export async function refreshWithToken(
  config: TokenIdpAuthConfig,
  payload: TokenEndpointPayload,
): Promise<unknown> {
  const url = withTenant(config.authority + (config.tokenPath || '/token'), config, 'auth');
  return httpPostJson(url, payload);
}

export async function revokeToken(
  config: TokenIdpAuthConfig,
  params: { accessToken: string; token: string; reasonRevoked?: string },
): Promise<unknown> {
  if (!config.authority || !params.accessToken || !params.token) {
    return null;
  }

  const url = withTenant(new URL(config.revokePath || '/revoke', config.authority).toString(), config, 'auth');
  const res = await fetch(url, {
    method: 'DELETE',
    headers: {
      Authorization: `Bearer ${params.accessToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      token: params.token,
      reasonRevoked: params.reasonRevoked || 'logout',
    }),
  });

  const data = await readResponseBody(res);

  if (!res.ok) {
    throw buildHttpError(data, res.status);
  }

  return data;
}

export function buildLogoutUrl(config: TokenIdpAuthConfig, origin = ''): string {
  if (!config.authority) {
    return '';
  }

  const url = new URL(
    withTenant(new URL(config.logoutPath || '/logout', config.authority).toString(), config, 'auth'),
  );

  if (config.clientId) {
    url.searchParams.set('client_id', config.clientId);
  }

  const postLogoutRedirectUri = resolvePostLogoutRedirectUri(config, origin);
  if (postLogoutRedirectUri) {
    url.searchParams.set('post_logout_redirect_uri', postLogoutRedirectUri);
  }

  return url.toString();
}

function resolvePostLogoutRedirectUri(config: TokenIdpAuthConfig, origin: string): string {
  const candidate = config.postLogoutRedirectUri;
  if (!candidate) {
    return '';
  }

  if (origin) {
    return new URL(candidate, origin).toString();
  }

  if (config.redirectUri) {
    return new URL(candidate, config.redirectUri).toString();
  }

  return String(candidate);
}
