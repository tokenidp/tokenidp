export type TokenIdpStorageMode = 'memory' | 'sessionStorage' | 'localStorage';
export type TokenIdpTenantPropagationMode = 'all' | 'api' | 'none';

export interface TokenIdpAuthConfig {
  authority: string;
  clientId: string;
  tenantKey?: string;
  tenantPropagationMode?: TokenIdpTenantPropagationMode;
  tenantQueryParameter?: string;
  tenantHeaderName?: string;
  tenantKeyStorageKey?: string;
  redirectUri: string;
  postLoginRedirectUri?: string;
  postLogoutRedirectUri?: string;
  scope?: string;
  audience?: string;
  authorizePath?: string;
  tokenPath?: string;
  revokePath?: string;
  logoutPath?: string;
  storage?: TokenIdpStorageMode;
  storageKey?: string;
  pkceVerifierKey?: string;
  oauthStateKey?: string;
  autoRefresh?: boolean;
  refreshSkewSeconds?: number;
}

export interface TokenIdpLoginOptions {
  prompt?: string;
  loginHint?: string;
  audience?: string;
  tenantKey?: string;
}

export interface TokenIdpCallbackParams {
  code: string;
  state: string;
}

export interface TokenIdpAuthState {
  isAuthenticated: boolean;
  tenantKey: string;
  landingPage: string;
  accessToken: string;
  refreshToken: string;
  idToken: string;
  expiresAt: number;
  error: string;
}

export interface TokenIdpTokenResult {
  tenantKey: string;
  accessToken: string;
  refreshToken: string;
  idToken: string;
  expiresAt: number;
}

export interface TokenIdpTokenParts {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  idToken: string;
}
