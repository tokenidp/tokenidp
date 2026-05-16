import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { TOKEN_IDP_AUTH_CONFIG } from './auth.config';
import { buildLogoutUrl, exchangeAuthorizationCode, extractToken, refreshWithToken, revokeToken } from './auth-api';
import {
  TokenIdpAuthState,
  TokenIdpCallbackParams,
  TokenIdpLoginOptions,
  TokenIdpTokenResult,
} from './models';
import { buildAuthorizeUrl, randomState } from './oauth';
import { generateCodeChallenge, generateCodeVerifier } from './pkce';
import { createMemoryStorage, TokenIdpStorage } from './storage';
import { normalizeTenantPropagationMode, resolveApiTenantKey, resolveAuthTenantKey } from './tenant';

const initialState: TokenIdpAuthState = {
  isAuthenticated: false,
  tenantKey: '',
  landingPage: '',
  accessToken: '',
  refreshToken: '',
  idToken: '',
  expiresAt: 0,
  error: '',
};

@Injectable({ providedIn: 'root' })
export class TokenIdpAuthService {
  private readonly injectedConfig = inject(TOKEN_IDP_AUTH_CONFIG);
  private readonly config = {
    ...this.injectedConfig,
    tenantPropagationMode: normalizeTenantPropagationMode(this.injectedConfig.tenantPropagationMode),
  };
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly storage = this.createStorage();
  private refreshTimer: ReturnType<typeof setTimeout> | null = null;
  private refreshInFlight = false;
  private readonly stateSignal = signal<TokenIdpAuthState>(this.buildInitialState());

  readonly state = this.stateSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.stateSignal().isAuthenticated);
  readonly accessToken = computed(() => this.stateSignal().accessToken);
  readonly refreshToken = computed(() => this.stateSignal().refreshToken);
  readonly idToken = computed(() => this.stateSignal().idToken);
  readonly expiresAt = computed(() => this.stateSignal().expiresAt);
  readonly tenantKey = computed(() => this.stateSignal().tenantKey);
  readonly error = computed(() => this.stateSignal().error);
  readonly landingPage = computed(() => this.stateSignal().landingPage);

  constructor() {
    this.persistState(this.stateSignal());
    this.scheduleAutoRefresh(this.stateSignal().expiresAt);
  }

  async login(options: TokenIdpLoginOptions = {}): Promise<void> {
    if (!this.isBrowser) {
      return;
    }

    if (!this.config.authority || !this.config.clientId || !this.config.redirectUri) {
      throw new Error('Missing authority/clientId/redirectUri in TokenIDP auth config.');
    }

    const verifier = generateCodeVerifier();
    const challenge = await generateCodeChallenge(verifier);
    const stateVal = randomState();
    const locationSearch = this.document.defaultView?.location.search || '';
    const authorizeTenantKey = resolveAuthTenantKey(this.config, options, locationSearch);
    const apiTenantKey = resolveApiTenantKey(this.config, options, locationSearch);

    sessionStorage.setItem(this.config.pkceVerifierKey || 'idp_pkce_verifier', verifier);
    sessionStorage.setItem(this.config.oauthStateKey || 'idp_oauth_state', stateVal);

    if (apiTenantKey) {
      sessionStorage.setItem(this.config.tenantKeyStorageKey || 'idp_tenant_key', apiTenantKey);
    } else {
      sessionStorage.removeItem(this.config.tenantKeyStorageKey || 'idp_tenant_key');
    }

    const authorizeUrl = buildAuthorizeUrl(this.config, {
      ...options,
      codeChallenge: challenge,
      state: stateVal,
      tenantKey: authorizeTenantKey,
    });

    this.document.defaultView?.location.assign(authorizeUrl);
  }

  async logout(): Promise<void> {
    const state = this.stateSignal();
    const logoutUrl = buildLogoutUrl(this.config, this.document.defaultView?.location.origin || '');
    this.clearRefreshTimer();

    try {
      await revokeToken(this.config, {
        accessToken: state.accessToken,
        token: state.refreshToken,
        reasonRevoked: 'logout',
      });
    } catch (error) {
      console.warn('Token revocation during logout failed.', error);
    }

    if (this.isBrowser && logoutUrl) {
      this.document.defaultView?.addEventListener('pagehide', () => this.clearLocalSession(), {
        once: true,
      });
      this.document.defaultView?.location.assign(logoutUrl);
      return;
    }

    this.clearLocalSession();
  }

  async handleCallback(params: TokenIdpCallbackParams): Promise<TokenIdpTokenResult> {
    if (!this.isBrowser) {
      throw new Error('OAuth callback handling requires a browser.');
    }

    const verifier = sessionStorage.getItem(this.config.pkceVerifierKey || 'idp_pkce_verifier');
    if (!verifier) {
      throw new Error('Missing code verifier (PKCE).');
    }

    const expectedState = sessionStorage.getItem(this.config.oauthStateKey || 'idp_oauth_state');
    if (expectedState && params.state && expectedState !== params.state) {
      throw new Error('Invalid OAuth state. Possible CSRF.');
    }

    const locationSearch = this.document.defaultView?.location.search || '';
    const tenantKey = resolveApiTenantKey(this.config, {}, locationSearch);
    const tokenPayload = await exchangeAuthorizationCode(this.config, {
      grantType: 'authorization_code',
      clientId: this.config.clientId,
      redirectUri: this.config.redirectUri,
      code: params.code,
      codeVerifier: verifier,
      scope: this.config.scope,
    });

    const { accessToken, refreshToken, expiresIn, idToken } = extractToken(tokenPayload);
    if (!accessToken) {
      throw new Error('Token response did not include an access token.');
    }

    const expiresAt = expiresIn ? Date.now() + expiresIn * 1000 : 0;
    const result = {
      tenantKey,
      accessToken,
      refreshToken: refreshToken || '',
      idToken: idToken || '',
      expiresAt,
    };

    this.updateState({
      ...result,
      isAuthenticated: true,
      error: '',
      landingPage: this.config.postLoginRedirectUri || '/',
    });

    sessionStorage.removeItem(this.config.pkceVerifierKey || 'idp_pkce_verifier');
    sessionStorage.removeItem(this.config.oauthStateKey || 'idp_oauth_state');

    return result;
  }

  async refresh(): Promise<Omit<TokenIdpTokenResult, 'tenantKey'>> {
    const state = this.stateSignal();
    if (!state.refreshToken) {
      throw new Error('No refresh token available.');
    }

    const tokenPayload = await refreshWithToken(this.config, {
      grantType: 'refresh_token',
      clientId: this.config.clientId,
      refreshToken: state.refreshToken,
      scope: this.config.scope,
    });

    const { accessToken, refreshToken, expiresIn, idToken } = extractToken(tokenPayload);
    if (!accessToken) {
      throw new Error('Refresh response did not include an access token.');
    }

    const expiresAt = expiresIn ? Date.now() + expiresIn * 1000 : 0;
    const result = {
      accessToken,
      refreshToken: refreshToken || state.refreshToken,
      idToken: idToken || state.idToken,
      expiresAt,
    };

    this.updateState(result);
    return result;
  }

  setError(message: string): void {
    this.updateState({ error: message || 'Unknown error' });
  }

  private updateState(partial: Partial<TokenIdpAuthState>): void {
    this.stateSignal.update((current) => {
      const next = { ...current, ...partial };
      this.persistState(next);
      this.scheduleAutoRefresh(next.expiresAt);
      return next;
    });
  }

  private buildInitialState(): TokenIdpAuthState {
    const persistedRaw = this.storage.getItem(this.config.storageKey || 'idp_user');
    const persisted = persistedRaw ? safeJsonParse(persistedRaw) : null;
    const locationSearch = this.document.defaultView?.location.search || '';
    const tenantKey = resolveApiTenantKey(
      {
        ...this.config,
        tenantKey: persisted?.tenantKey || this.config.tenantKey,
      },
      {},
      locationSearch,
    );

    if (!persisted) {
      return { ...initialState, tenantKey };
    }

    return {
      ...initialState,
      isAuthenticated: !!persisted.isAuthenticated,
      landingPage: persisted.landingPage || '',
      accessToken: persisted.accessToken || '',
      refreshToken: persisted.refreshToken || '',
      idToken: persisted.idToken || '',
      expiresAt: persisted.expiresAt || 0,
      error: persisted.error || '',
      tenantKey,
    };
  }

  private persistState(state: TokenIdpAuthState): void {
    this.storage.setItem(this.config.storageKey || 'idp_user', JSON.stringify(state));
  }

  private createStorage(): TokenIdpStorage {
    if (!this.isBrowser) {
      return createMemoryStorage();
    }

    if (this.config.storage === 'localStorage') {
      return this.document.defaultView?.localStorage || createMemoryStorage();
    }

    if (this.config.storage === 'sessionStorage') {
      return this.document.defaultView?.sessionStorage || createMemoryStorage();
    }

    return createMemoryStorage();
  }

  private clearLocalSession(): void {
    this.storage.removeItem(this.config.storageKey || 'idp_user');

    if (this.isBrowser) {
      sessionStorage.removeItem(this.config.pkceVerifierKey || 'idp_pkce_verifier');
      sessionStorage.removeItem(this.config.oauthStateKey || 'idp_oauth_state');
      sessionStorage.removeItem(this.config.tenantKeyStorageKey || 'idp_tenant_key');
    }

    this.stateSignal.set({ ...initialState });
  }

  private scheduleAutoRefresh(nextExpiresAtMs: number): void {
    this.clearRefreshTimer();
    if (!this.config.autoRefresh || !nextExpiresAtMs || !this.isBrowser) {
      return;
    }

    const skewMs = (this.config.refreshSkewSeconds || 60) * 1000;
    const delay = Math.max(0, nextExpiresAtMs - Date.now() - skewMs);

    this.refreshTimer = setTimeout(async () => {
      if (this.refreshInFlight) {
        return;
      }

      this.refreshInFlight = true;
      const ok = await this.tryRefreshWithRetry(1, 5000);
      this.refreshInFlight = false;

      if (!ok) {
        await this.logout();
      }
    }, delay);
  }

  private clearRefreshTimer(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  private async tryRefreshWithRetry(retries: number, retryDelayMs: number): Promise<boolean> {
    try {
      await this.refresh();
      return true;
    } catch {
      if (retries > 0) {
        await new Promise((resolve) => setTimeout(resolve, retryDelayMs));
        return this.tryRefreshWithRetry(retries - 1, retryDelayMs);
      }

      return false;
    }
  }
}

function safeJsonParse(raw: string): Partial<TokenIdpAuthState> | null {
  try {
    return JSON.parse(raw) as Partial<TokenIdpAuthState>;
  } catch {
    return null;
  }
}
