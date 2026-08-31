import * as i1 from '@angular/common';
import { DOCUMENT, isPlatformBrowser, CommonModule } from '@angular/common';
import * as i0 from '@angular/core';
import { InjectionToken, makeEnvironmentProviders, inject, PLATFORM_ID, signal, computed, Injectable, Input, Component } from '@angular/core';
import { Router } from '@angular/router';

const defaultAuthConfig = {
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
const TOKEN_IDP_AUTH_CONFIG = new InjectionToken('TOKEN_IDP_AUTH_CONFIG', {
    factory: () => defaultAuthConfig,
});
function provideTokenIdpAuth(config) {
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

function normalizeTenantPropagationMode(value) {
    const normalized = String(value || 'all').trim().toLowerCase();
    if (normalized === 'none' || normalized === 'api') {
        return normalized;
    }
    return 'all';
}
function getAuthTenantKey(config) {
    return normalizeTenantPropagationMode(config.tenantPropagationMode) === 'all'
        ? String(config.tenantKey || '').trim()
        : '';
}
function getApiTenantKey(config) {
    const mode = normalizeTenantPropagationMode(config.tenantPropagationMode);
    return mode === 'all' || mode === 'api' ? String(config.tenantKey || '').trim() : '';
}
function resolveRawTenantKey(config, overrides = {}, locationSearch = '') {
    const explicitTenantKey = String(overrides.tenantKey || config.tenantKey || '').trim();
    if (explicitTenantKey) {
        return explicitTenantKey;
    }
    const tenantFromQuery = new URLSearchParams(locationSearch).get(config.tenantQueryParameter || 'tenant');
    if (tenantFromQuery) {
        return tenantFromQuery.trim();
    }
    const tenantFromStorage = globalThis.sessionStorage?.getItem(config.tenantKeyStorageKey || 'idp_tenant_key');
    if (tenantFromStorage) {
        return tenantFromStorage.trim();
    }
    return '';
}
function resolveAuthTenantKey(config, overrides = {}, locationSearch = '') {
    return normalizeTenantPropagationMode(config.tenantPropagationMode) === 'all'
        ? resolveRawTenantKey(config, overrides, locationSearch)
        : '';
}
function resolveApiTenantKey(config, overrides = {}, locationSearch = '') {
    const mode = normalizeTenantPropagationMode(config.tenantPropagationMode);
    return mode === 'all' || mode === 'api' ? resolveRawTenantKey(config, overrides, locationSearch) : '';
}

async function httpPostJson(url, body) {
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
async function readResponseBody(res) {
    const text = await res.text();
    if (!text) {
        return null;
    }
    try {
        return JSON.parse(text);
    }
    catch {
        return text;
    }
}
function buildHttpError(data, status) {
    const err = new Error(getErrorMessage(data, status));
    err.status = status;
    err.data = data;
    return err;
}
function getErrorMessage(data, status) {
    if (!data || typeof data !== 'object') {
        return `HTTP ${status}`;
    }
    const record = data;
    const value = record['value'];
    const directError = record['error'];
    const wrappedError = directError && typeof directError === 'object'
        ? directError
        : value?.['error'] && typeof value['error'] === 'object'
            ? value['error']
            : null;
    return (stringValue(record['error_description']) ||
        stringValue(wrappedError?.['error']) ||
        stringValue(wrappedError?.['Error']) ||
        stringValue(wrappedError?.['message']) ||
        stringValue(wrappedError?.['Message']) ||
        stringValue(directError) ||
        stringValue(record['message']) ||
        `HTTP ${status}`);
}
function stringValue(value) {
    return typeof value === 'string' ? value : '';
}
function withTenant(url, config, target = 'api') {
    const tenantKey = target === 'auth' ? getAuthTenantKey(config) : getApiTenantKey(config);
    if (!tenantKey) {
        return url;
    }
    const tenantUrl = new URL(url);
    tenantUrl.searchParams.set(config.tenantQueryParameter || 'tenant', tenantKey);
    return tenantUrl.toString();
}
function extractToken(tokenPayload) {
    if (!tokenPayload || typeof tokenPayload !== 'object') {
        return { accessToken: '', refreshToken: '', expiresIn: 0, idToken: '' };
    }
    const record = tokenPayload;
    const value = (record['value'] || record);
    const accessToken = stringValue(value['accessToken']) || stringValue(value['access_token']);
    const refreshToken = stringValue(value['refreshToken']) || stringValue(value['refresh_token']);
    const expiresIn = Number(value['expiresIn'] || value['expires_in'] || 0) || 0;
    const idToken = stringValue(value['idToken']) ||
        stringValue(value['id_token']) ||
        stringValue(record['idToken']) ||
        stringValue(record['id_token']);
    return { accessToken, refreshToken, expiresIn, idToken };
}
async function exchangeAuthorizationCode(config, payload) {
    const url = withTenant(config.authority + (config.tokenPath || '/token'), config, 'auth');
    return httpPostJson(url, payload);
}
async function refreshWithToken(config, payload) {
    const url = withTenant(config.authority + (config.tokenPath || '/token'), config, 'auth');
    return httpPostJson(url, payload);
}
async function revokeToken(config, params) {
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
function buildLogoutUrl(config, origin = '') {
    if (!config.authority) {
        return '';
    }
    const url = new URL(withTenant(new URL(config.logoutPath || '/logout', config.authority).toString(), config, 'auth'));
    if (config.clientId) {
        url.searchParams.set('client_id', config.clientId);
    }
    const postLogoutRedirectUri = resolvePostLogoutRedirectUri(config, origin);
    if (postLogoutRedirectUri) {
        url.searchParams.set('post_logout_redirect_uri', postLogoutRedirectUri);
    }
    return url.toString();
}
function resolvePostLogoutRedirectUri(config, origin) {
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

function randomState(length = 32) {
    const bytes = new Uint8Array(length);
    crypto.getRandomValues(bytes);
    return Array.from(bytes)
        .map((byte) => byte.toString(16).padStart(2, '0'))
        .join('');
}
function buildAuthorizeUrl(config, params) {
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

function base64UrlEncode(arrayBuffer) {
    const bytes = new Uint8Array(arrayBuffer);
    let str = '';
    for (let i = 0; i < bytes.byteLength; i += 1) {
        str += String.fromCharCode(bytes[i]);
    }
    return btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}
function generateCodeVerifier(length = 64) {
    const charset = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~';
    const randomValues = new Uint8Array(length);
    crypto.getRandomValues(randomValues);
    let verifier = '';
    for (let i = 0; i < randomValues.length; i += 1) {
        verifier += charset[randomValues[i] % charset.length];
    }
    return verifier;
}
async function generateCodeChallenge(verifier) {
    const enc = new TextEncoder();
    const data = enc.encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', data);
    return base64UrlEncode(digest);
}

function createMemoryStorage() {
    let mem = {};
    return {
        getItem: (key) => (key in mem ? mem[key] : null),
        setItem: (key, value) => {
            mem[key] = String(value);
        },
        removeItem: (key) => {
            delete mem[key];
        },
        clear: () => {
            mem = {};
        },
    };
}

const initialState = {
    isAuthenticated: false,
    tenantKey: '',
    landingPage: '',
    accessToken: '',
    refreshToken: '',
    idToken: '',
    expiresAt: 0,
    error: '',
};
class TokenIdpAuthService {
    injectedConfig = inject(TOKEN_IDP_AUTH_CONFIG);
    config = {
        ...this.injectedConfig,
        tenantPropagationMode: normalizeTenantPropagationMode(this.injectedConfig.tenantPropagationMode),
    };
    platformId = inject(PLATFORM_ID);
    document = inject(DOCUMENT);
    isBrowser = isPlatformBrowser(this.platformId);
    storage = this.createStorage();
    refreshTimer = null;
    refreshInFlight = false;
    stateSignal = signal(this.buildInitialState(), ...(ngDevMode ? [{ debugName: "stateSignal" }] : /* istanbul ignore next */ []));
    state = this.stateSignal.asReadonly();
    isAuthenticated = computed(() => this.stateSignal().isAuthenticated, ...(ngDevMode ? [{ debugName: "isAuthenticated" }] : /* istanbul ignore next */ []));
    accessToken = computed(() => this.stateSignal().accessToken, ...(ngDevMode ? [{ debugName: "accessToken" }] : /* istanbul ignore next */ []));
    refreshToken = computed(() => this.stateSignal().refreshToken, ...(ngDevMode ? [{ debugName: "refreshToken" }] : /* istanbul ignore next */ []));
    idToken = computed(() => this.stateSignal().idToken, ...(ngDevMode ? [{ debugName: "idToken" }] : /* istanbul ignore next */ []));
    expiresAt = computed(() => this.stateSignal().expiresAt, ...(ngDevMode ? [{ debugName: "expiresAt" }] : /* istanbul ignore next */ []));
    tenantKey = computed(() => this.stateSignal().tenantKey, ...(ngDevMode ? [{ debugName: "tenantKey" }] : /* istanbul ignore next */ []));
    error = computed(() => this.stateSignal().error, ...(ngDevMode ? [{ debugName: "error" }] : /* istanbul ignore next */ []));
    landingPage = computed(() => this.stateSignal().landingPage, ...(ngDevMode ? [{ debugName: "landingPage" }] : /* istanbul ignore next */ []));
    constructor() {
        this.persistState(this.stateSignal());
        this.scheduleAutoRefresh(this.stateSignal().expiresAt);
    }
    async login(options = {}) {
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
        }
        else {
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
    async logout() {
        const state = this.stateSignal();
        const logoutUrl = buildLogoutUrl(this.config, this.document.defaultView?.location.origin || '');
        this.clearRefreshTimer();
        try {
            await revokeToken(this.config, {
                accessToken: state.accessToken,
                token: state.refreshToken,
                reasonRevoked: 'logout',
            });
        }
        catch (error) {
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
    async handleCallback(params) {
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
    async refresh() {
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
    setError(message) {
        this.updateState({ error: message || 'Unknown error' });
    }
    updateState(partial) {
        this.stateSignal.update((current) => {
            const next = { ...current, ...partial };
            this.persistState(next);
            this.scheduleAutoRefresh(next.expiresAt);
            return next;
        });
    }
    buildInitialState() {
        const persistedRaw = this.storage.getItem(this.config.storageKey || 'idp_user');
        const persisted = persistedRaw ? safeJsonParse(persistedRaw) : null;
        const locationSearch = this.document.defaultView?.location.search || '';
        const tenantKey = resolveApiTenantKey({
            ...this.config,
            tenantKey: persisted?.tenantKey || this.config.tenantKey,
        }, {}, locationSearch);
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
    persistState(state) {
        this.storage.setItem(this.config.storageKey || 'idp_user', JSON.stringify(state));
    }
    createStorage() {
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
    clearLocalSession() {
        this.storage.removeItem(this.config.storageKey || 'idp_user');
        if (this.isBrowser) {
            sessionStorage.removeItem(this.config.pkceVerifierKey || 'idp_pkce_verifier');
            sessionStorage.removeItem(this.config.oauthStateKey || 'idp_oauth_state');
            sessionStorage.removeItem(this.config.tenantKeyStorageKey || 'idp_tenant_key');
        }
        this.stateSignal.set({ ...initialState });
    }
    scheduleAutoRefresh(nextExpiresAtMs) {
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
    clearRefreshTimer() {
        if (this.refreshTimer) {
            clearTimeout(this.refreshTimer);
            this.refreshTimer = null;
        }
    }
    async tryRefreshWithRetry(retries, retryDelayMs) {
        try {
            await this.refresh();
            return true;
        }
        catch {
            if (retries > 0) {
                await new Promise((resolve) => setTimeout(resolve, retryDelayMs));
                return this.tryRefreshWithRetry(retries - 1, retryDelayMs);
            }
            return false;
        }
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: TokenIdpAuthService, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: TokenIdpAuthService, providedIn: 'root' });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: TokenIdpAuthService, decorators: [{
            type: Injectable,
            args: [{ providedIn: 'root' }]
        }], ctorParameters: () => [] });
function safeJsonParse(raw) {
    try {
        return JSON.parse(raw);
    }
    catch {
        return null;
    }
}

class IdpAuthCallbackComponent {
    redirectTo = '';
    logoUrl = '';
    logoAlt = 'Application logo';
    fallbackBadge = 'ID';
    error = '';
    auth = inject(TokenIdpAuthService);
    router = inject(Router);
    async ngOnInit() {
        const qs = new URLSearchParams(window.location.search);
        const code = qs.get('code');
        const state = qs.get('state');
        const err = qs.get('error');
        const errDesc = qs.get('error_description');
        if (err) {
            this.setError(errDesc || err || 'Authentication error.');
            return;
        }
        if (!code || !state) {
            this.setError('Missing authorization code or state.');
            return;
        }
        try {
            await this.auth.handleCallback({ code, state });
            await this.router.navigateByUrl(this.redirectTo || this.auth.landingPage() || '/', {
                replaceUrl: true,
            });
        }
        catch (error) {
            this.setError(error instanceof Error ? error.message : 'Login callback failed.');
        }
    }
    setError(message) {
        this.error = message;
        this.auth.setError(message);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: IdpAuthCallbackComponent, deps: [], target: i0.ɵɵFactoryTarget.Component });
    static ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "21.2.13", type: IdpAuthCallbackComponent, isStandalone: true, selector: "tokenidp-auth-callback", inputs: { redirectTo: "redirectTo", logoUrl: "logoUrl", logoAlt: "logoAlt", fallbackBadge: "fallbackBadge" }, ngImport: i0, template: `
    <div class="idp-login-page">
      <section class="idp-login-section-container">
        <div class="idp-login-section idp-redirect-card">
          <div class="idp-content">
            <ng-container *ngIf="logoUrl; else fallbackLogo">
              <div class="idp-logo-image-wrap">
                <img [src]="logoUrl" [alt]="logoAlt" width="250" />
              </div>
            </ng-container>
            <ng-template #fallbackLogo>
              <div class="idp-logo">{{ fallbackBadge }}</div>
            </ng-template>
            <h1>Completing sign-in</h1>
            <p>{{ error || 'Please wait while we finish authentication.' }}</p>
          </div>
        </div>
      </section>
    </div>
  `, isInline: true, styles: [".idp-login-page{min-height:100vh;display:grid;place-items:center;background:#f5f7fb;color:#151923;font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,Segoe UI,sans-serif}.idp-login-section-container{width:min(440px,calc(100vw - 32px))}.idp-login-section{background:#fff;border:1px solid #e4e8f0;border-radius:8px;box-shadow:0 18px 45px #0f172a14}.idp-content{padding:32px;text-align:center}.idp-logo,.idp-logo-image-wrap{margin:0 auto 16px}.idp-logo{width:56px;height:56px;display:grid;place-items:center;border-radius:8px;background:#111827;color:#fff;font-weight:700}h1{margin:0 0 8px;font-size:24px;line-height:1.25}p{margin:0;color:#667085;line-height:1.5}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1.NgIf, selector: "[ngIf]", inputs: ["ngIf", "ngIfThen", "ngIfElse"] }] });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: IdpAuthCallbackComponent, decorators: [{
            type: Component,
            args: [{ selector: 'tokenidp-auth-callback', standalone: true, imports: [CommonModule], template: `
    <div class="idp-login-page">
      <section class="idp-login-section-container">
        <div class="idp-login-section idp-redirect-card">
          <div class="idp-content">
            <ng-container *ngIf="logoUrl; else fallbackLogo">
              <div class="idp-logo-image-wrap">
                <img [src]="logoUrl" [alt]="logoAlt" width="250" />
              </div>
            </ng-container>
            <ng-template #fallbackLogo>
              <div class="idp-logo">{{ fallbackBadge }}</div>
            </ng-template>
            <h1>Completing sign-in</h1>
            <p>{{ error || 'Please wait while we finish authentication.' }}</p>
          </div>
        </div>
      </section>
    </div>
  `, styles: [".idp-login-page{min-height:100vh;display:grid;place-items:center;background:#f5f7fb;color:#151923;font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,Segoe UI,sans-serif}.idp-login-section-container{width:min(440px,calc(100vw - 32px))}.idp-login-section{background:#fff;border:1px solid #e4e8f0;border-radius:8px;box-shadow:0 18px 45px #0f172a14}.idp-content{padding:32px;text-align:center}.idp-logo,.idp-logo-image-wrap{margin:0 auto 16px}.idp-logo{width:56px;height:56px;display:grid;place-items:center;border-radius:8px;background:#111827;color:#fff;font-weight:700}h1{margin:0 0 8px;font-size:24px;line-height:1.25}p{margin:0;color:#667085;line-height:1.5}\n"] }]
        }], propDecorators: { redirectTo: [{
                type: Input
            }], logoUrl: [{
                type: Input
            }], logoAlt: [{
                type: Input
            }], fallbackBadge: [{
                type: Input
            }] } });

class IdpLoginComponent {
    logoUrl = '';
    logoAlt = 'Application logo';
    title = 'Redirecting to sign-in...';
    subtitle = 'Please wait while we securely connect to Identity.';
    signedOutTitle = 'You have been signed out';
    signedOutSubtitle = 'Start a new session when you are ready.';
    signInAgainLabel = 'Sign in again';
    fallbackBadge = 'ID';
    loginOptions;
    isLoggedOut = false;
    auth = inject(TokenIdpAuthService);
    ngOnInit() {
        this.isLoggedOut = new URLSearchParams(window.location.search).get('logged_out') === '1';
        if (!this.isLoggedOut) {
            void this.auth.login(this.loginOptions);
        }
    }
    signInAgain() {
        void this.auth.login(this.loginOptions);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: IdpLoginComponent, deps: [], target: i0.ɵɵFactoryTarget.Component });
    static ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "21.2.13", type: IdpLoginComponent, isStandalone: true, selector: "tokenidp-login", inputs: { logoUrl: "logoUrl", logoAlt: "logoAlt", title: "title", subtitle: "subtitle", signedOutTitle: "signedOutTitle", signedOutSubtitle: "signedOutSubtitle", signInAgainLabel: "signInAgainLabel", fallbackBadge: "fallbackBadge", loginOptions: "loginOptions" }, ngImport: i0, template: `
    <div class="idp-login-page">
      <section class="idp-login-section-container">
        <div class="idp-login-section idp-redirect-card">
          <div class="idp-content">
            <ng-container *ngIf="logoUrl; else fallbackLogo">
              <div class="idp-logo-image-wrap">
                <img [src]="logoUrl" [alt]="logoAlt" width="250" />
              </div>
            </ng-container>
            <ng-template #fallbackLogo>
              <div class="idp-logo">{{ fallbackBadge }}</div>
            </ng-template>

            <ng-container *ngIf="isLoggedOut; else redirecting">
              <h1>{{ signedOutTitle }}</h1>
              <p>{{ signedOutSubtitle }}</p>
              <button type="button" class="idp-button" (click)="signInAgain()">
                {{ signInAgainLabel }}
              </button>
            </ng-container>

            <ng-template #redirecting>
              <h1>{{ title }}</h1>
              <p>{{ subtitle }}</p>
              <div class="idp-spinner" aria-hidden="true"></div>
            </ng-template>
          </div>
        </div>
      </section>
    </div>
  `, isInline: true, styles: [".idp-login-page{min-height:100vh;display:grid;place-items:center;background:#f5f7fb;color:#151923;font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,Segoe UI,sans-serif}.idp-login-section-container{width:min(440px,calc(100vw - 32px))}.idp-login-section{background:#fff;border:1px solid #e4e8f0;border-radius:8px;box-shadow:0 18px 45px #0f172a14}.idp-content{padding:32px;text-align:center}.idp-logo,.idp-logo-image-wrap{margin:0 auto 16px}.idp-logo{width:56px;height:56px;display:grid;place-items:center;border-radius:8px;background:#111827;color:#fff;font-weight:700}h1{margin:0 0 8px;font-size:24px;line-height:1.25}p{margin:0;color:#667085;line-height:1.5}.idp-button{margin-top:20px;min-height:40px;border:0;border-radius:6px;padding:0 16px;background:#0f62fe;color:#fff;font-weight:600;cursor:pointer}.idp-spinner{width:28px;height:28px;border:3px solid #d0d5dd;border-top-color:#0f62fe;border-radius:50%;margin:20px auto 0;animation:idp-spin .8s linear infinite}@keyframes idp-spin{to{transform:rotate(360deg)}}\n"], dependencies: [{ kind: "ngmodule", type: CommonModule }, { kind: "directive", type: i1.NgIf, selector: "[ngIf]", inputs: ["ngIf", "ngIfThen", "ngIfElse"] }] });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "21.2.13", ngImport: i0, type: IdpLoginComponent, decorators: [{
            type: Component,
            args: [{ selector: 'tokenidp-login', standalone: true, imports: [CommonModule], template: `
    <div class="idp-login-page">
      <section class="idp-login-section-container">
        <div class="idp-login-section idp-redirect-card">
          <div class="idp-content">
            <ng-container *ngIf="logoUrl; else fallbackLogo">
              <div class="idp-logo-image-wrap">
                <img [src]="logoUrl" [alt]="logoAlt" width="250" />
              </div>
            </ng-container>
            <ng-template #fallbackLogo>
              <div class="idp-logo">{{ fallbackBadge }}</div>
            </ng-template>

            <ng-container *ngIf="isLoggedOut; else redirecting">
              <h1>{{ signedOutTitle }}</h1>
              <p>{{ signedOutSubtitle }}</p>
              <button type="button" class="idp-button" (click)="signInAgain()">
                {{ signInAgainLabel }}
              </button>
            </ng-container>

            <ng-template #redirecting>
              <h1>{{ title }}</h1>
              <p>{{ subtitle }}</p>
              <div class="idp-spinner" aria-hidden="true"></div>
            </ng-template>
          </div>
        </div>
      </section>
    </div>
  `, styles: [".idp-login-page{min-height:100vh;display:grid;place-items:center;background:#f5f7fb;color:#151923;font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,Segoe UI,sans-serif}.idp-login-section-container{width:min(440px,calc(100vw - 32px))}.idp-login-section{background:#fff;border:1px solid #e4e8f0;border-radius:8px;box-shadow:0 18px 45px #0f172a14}.idp-content{padding:32px;text-align:center}.idp-logo,.idp-logo-image-wrap{margin:0 auto 16px}.idp-logo{width:56px;height:56px;display:grid;place-items:center;border-radius:8px;background:#111827;color:#fff;font-weight:700}h1{margin:0 0 8px;font-size:24px;line-height:1.25}p{margin:0;color:#667085;line-height:1.5}.idp-button{margin-top:20px;min-height:40px;border:0;border-radius:6px;padding:0 16px;background:#0f62fe;color:#fff;font-weight:600;cursor:pointer}.idp-spinner{width:28px;height:28px;border:3px solid #d0d5dd;border-top-color:#0f62fe;border-radius:50%;margin:20px auto 0;animation:idp-spin .8s linear infinite}@keyframes idp-spin{to{transform:rotate(360deg)}}\n"] }]
        }], propDecorators: { logoUrl: [{
                type: Input
            }], logoAlt: [{
                type: Input
            }], title: [{
                type: Input
            }], subtitle: [{
                type: Input
            }], signedOutTitle: [{
                type: Input
            }], signedOutSubtitle: [{
                type: Input
            }], signInAgainLabel: [{
                type: Input
            }], fallbackBadge: [{
                type: Input
            }], loginOptions: [{
                type: Input
            }] } });

/**
 * Generated bundle index. Do not edit.
 */

export { IdpAuthCallbackComponent, IdpLoginComponent, TOKEN_IDP_AUTH_CONFIG, TokenIdpAuthService, defaultAuthConfig, provideTokenIdpAuth };
//# sourceMappingURL=tokenidp-angular.mjs.map
