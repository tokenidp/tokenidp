import { TokenIdpAuthConfig, TokenIdpLoginOptions } from './models';
import { getAuthTenantKey, normalizeTenantPropagationMode } from './tenant';

export function randomState(length = 32): string {
  const bytes = new Uint8Array(length);
  crypto.getRandomValues(bytes);
  return Array.from(bytes)
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');
}

export function buildAuthorizeUrl(
  config: TokenIdpAuthConfig,
  params: TokenIdpLoginOptions & { codeChallenge: string; state: string },
): string {
  const url = new URL(config.authority + (config.authorizePath || '/authorize'));
  const tenantKey = getAuthTenantKey({
    ...config,
    tenantPropagationMode: normalizeTenantPropagationMode(config.tenantPropagationMode),
    tenantKey: params.tenantKey || config.tenantKey,
  });

  url.searchParams.set('response_type', 'code');
  url.searchParams.set('client_id', config.clientId);
  url.searchParams.set('redirect_uri', config.redirectUri);
  url.searchParams.set('scope', config.scope || 'openid profile offline_access');
  url.searchParams.set('code_challenge', params.codeChallenge);
  url.searchParams.set('code_challenge_method', 'S256');
  url.searchParams.set('state', params.state);

  if (params.audience || config.audience) {
    url.searchParams.set('audience', params.audience || config.audience || '');
  }

  if (tenantKey) {
    url.searchParams.set(config.tenantQueryParameter || 'tenant', tenantKey);
  }

  if (params.prompt) {
    url.searchParams.set('prompt', params.prompt);
  }

  if (params.loginHint) {
    url.searchParams.set('login_hint', params.loginHint);
  }

  return url.toString();
}
