import * as _angular_core from '@angular/core';
import { OnInit, InjectionToken, EnvironmentProviders } from '@angular/core';

declare class IdpAuthCallbackComponent implements OnInit {
    redirectTo: string;
    logoUrl: string;
    logoAlt: string;
    fallbackBadge: string;
    protected error: string;
    private readonly auth;
    private readonly router;
    ngOnInit(): Promise<void>;
    private setError;
    static ɵfac: _angular_core.ɵɵFactoryDeclaration<IdpAuthCallbackComponent, never>;
    static ɵcmp: _angular_core.ɵɵComponentDeclaration<IdpAuthCallbackComponent, "tokenidp-auth-callback", never, { "redirectTo": { "alias": "redirectTo"; "required": false; }; "logoUrl": { "alias": "logoUrl"; "required": false; }; "logoAlt": { "alias": "logoAlt"; "required": false; }; "fallbackBadge": { "alias": "fallbackBadge"; "required": false; }; }, {}, never, never, true, never>;
}

type TokenIdpStorageMode = 'memory' | 'sessionStorage' | 'localStorage';
type TokenIdpTenantPropagationMode = 'all' | 'api' | 'none';
interface TokenIdpAuthConfig {
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
interface TokenIdpLoginOptions {
    prompt?: string;
    loginHint?: string;
    audience?: string;
    tenantKey?: string;
}
interface TokenIdpCallbackParams {
    code: string;
    state: string;
}
interface TokenIdpAuthState {
    isAuthenticated: boolean;
    tenantKey: string;
    landingPage: string;
    accessToken: string;
    refreshToken: string;
    idToken: string;
    expiresAt: number;
    error: string;
}
interface TokenIdpTokenResult {
    tenantKey: string;
    accessToken: string;
    refreshToken: string;
    idToken: string;
    expiresAt: number;
}
interface TokenIdpTokenParts {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
    idToken: string;
}

declare const defaultAuthConfig: TokenIdpAuthConfig;
declare const TOKEN_IDP_AUTH_CONFIG: InjectionToken<TokenIdpAuthConfig>;
declare function provideTokenIdpAuth(config: Partial<TokenIdpAuthConfig>): EnvironmentProviders;

declare class TokenIdpAuthService {
    private readonly injectedConfig;
    private readonly config;
    private readonly platformId;
    private readonly document;
    private readonly isBrowser;
    private readonly storage;
    private refreshTimer;
    private refreshInFlight;
    private readonly stateSignal;
    readonly state: _angular_core.Signal<TokenIdpAuthState>;
    readonly isAuthenticated: _angular_core.Signal<boolean>;
    readonly accessToken: _angular_core.Signal<string>;
    readonly refreshToken: _angular_core.Signal<string>;
    readonly idToken: _angular_core.Signal<string>;
    readonly expiresAt: _angular_core.Signal<number>;
    readonly tenantKey: _angular_core.Signal<string>;
    readonly error: _angular_core.Signal<string>;
    readonly landingPage: _angular_core.Signal<string>;
    constructor();
    login(options?: TokenIdpLoginOptions): Promise<void>;
    logout(): Promise<void>;
    handleCallback(params: TokenIdpCallbackParams): Promise<TokenIdpTokenResult>;
    refresh(): Promise<Omit<TokenIdpTokenResult, 'tenantKey'>>;
    setError(message: string): void;
    private updateState;
    private buildInitialState;
    private persistState;
    private createStorage;
    private clearLocalSession;
    private scheduleAutoRefresh;
    private clearRefreshTimer;
    private tryRefreshWithRetry;
    static ɵfac: _angular_core.ɵɵFactoryDeclaration<TokenIdpAuthService, never>;
    static ɵprov: _angular_core.ɵɵInjectableDeclaration<TokenIdpAuthService>;
}

declare class IdpLoginComponent implements OnInit {
    logoUrl: string;
    logoAlt: string;
    title: string;
    subtitle: string;
    signedOutTitle: string;
    signedOutSubtitle: string;
    signInAgainLabel: string;
    fallbackBadge: string;
    loginOptions?: TokenIdpLoginOptions;
    protected isLoggedOut: boolean;
    private readonly auth;
    ngOnInit(): void;
    protected signInAgain(): void;
    static ɵfac: _angular_core.ɵɵFactoryDeclaration<IdpLoginComponent, never>;
    static ɵcmp: _angular_core.ɵɵComponentDeclaration<IdpLoginComponent, "tokenidp-login", never, { "logoUrl": { "alias": "logoUrl"; "required": false; }; "logoAlt": { "alias": "logoAlt"; "required": false; }; "title": { "alias": "title"; "required": false; }; "subtitle": { "alias": "subtitle"; "required": false; }; "signedOutTitle": { "alias": "signedOutTitle"; "required": false; }; "signedOutSubtitle": { "alias": "signedOutSubtitle"; "required": false; }; "signInAgainLabel": { "alias": "signInAgainLabel"; "required": false; }; "fallbackBadge": { "alias": "fallbackBadge"; "required": false; }; "loginOptions": { "alias": "loginOptions"; "required": false; }; }, {}, never, never, true, never>;
}

export { IdpAuthCallbackComponent, IdpLoginComponent, TOKEN_IDP_AUTH_CONFIG, TokenIdpAuthService, defaultAuthConfig, provideTokenIdpAuth };
export type { TokenIdpAuthConfig, TokenIdpAuthState, TokenIdpCallbackParams, TokenIdpLoginOptions, TokenIdpStorageMode, TokenIdpTenantPropagationMode, TokenIdpTokenParts, TokenIdpTokenResult };
