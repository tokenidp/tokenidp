import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';
import { TokenIdpAuthConfig } from './models';

export const defaultAuthConfig: TokenIdpAuthConfig = {
  authority: '',
  clientId: '',
  tenantKey: '',
  tenantPropagationMode: 'all',
  tenantQueryParameter: 'tenant',
  tenantHeaderName: 'X-Tenant-Key',
  tenantKeyStorageKey: 'idp_tenant_key',
  redirectUri: '',
  postLoginRedirectUri: '/',
  postLogoutRedirectUri: '/login',
  scope: 'openid profile offline_access',
  audience: '',
  authorizePath: '/authorize',
  tokenPath: '/token',
  revokePath: '/revoke',
  logoutPath: '/logout',
  storage: 'sessionStorage',
  storageKey: 'idp_user',
  pkceVerifierKey: 'idp_pkce_verifier',
  oauthStateKey: 'idp_oauth_state',
  autoRefresh: true,
  refreshSkewSeconds: 180,
};

export const TOKEN_IDP_AUTH_CONFIG = new InjectionToken<TokenIdpAuthConfig>(
  'TOKEN_IDP_AUTH_CONFIG',
  {
    factory: () => defaultAuthConfig,
  },
);

export function provideTokenIdpAuth(config: Partial<TokenIdpAuthConfig>): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: TOKEN_IDP_AUTH_CONFIG,
      useValue: {
        ...defaultAuthConfig,
        ...config,
      },
    },
  ]);
}
