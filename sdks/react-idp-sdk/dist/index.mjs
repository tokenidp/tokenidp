// src/AuthProvider.jsx
import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef
} from "react";

// src/config.js
var defaultAuthConfig = {
  authority: "",
  // e.g. https://idp.tokentresor.com
  clientId: "",
  tenantKey: "",
  tenantPropagationMode: "all",
  // all | api | none
  tenantQueryParameter: "tenant",
  tenantHeaderName: "X-Tenant-Key",
  tenantKeyStorageKey: "idp_tenant_key",
  redirectUri: "",
  // e.g. https://app.com/auth/callback
  postLoginRedirectUri: "/",
  // where to go after success
  postLogoutRedirectUri: "/login",
  // where to go after logout
  scope: "openid profile offline_access",
  audience: "",
  // optional
  // endpoints (default paths)
  authorizePath: "/authorize",
  tokenPath: "/token",
  revokePath: "/revoke",
  logoutPath: "/logout",
  userPermissionsPath: "/admin/user/permissions",
  // storage: "memory" | "sessionStorage" | "localStorage"
  storage: "sessionStorage",
  // keys
  storageKey: "idp_user",
  pkceVerifierKey: "idp_pkce_verifier",
  oauthStateKey: "idp_oauth_state",
  // refresh behavior
  autoRefresh: true,
  // refresh skew in seconds (refresh token slightly before expiry)
  refreshSkewSeconds: 180
};

// src/storage.js
function createStorage(mode) {
  if (mode === "localStorage") return window.localStorage;
  if (mode === "sessionStorage") return window.sessionStorage;
  let mem = {};
  return {
    getItem: (k) => k in mem ? mem[k] : null,
    setItem: (k, v) => {
      mem[k] = String(v);
    },
    removeItem: (k) => {
      delete mem[k];
    },
    clear: () => {
      mem = {};
    }
  };
}

// src/pkce.js
function base64UrlEncode(arrayBuffer) {
  const bytes = new Uint8Array(arrayBuffer);
  let str = "";
  for (let i = 0; i < bytes.byteLength; i++)
    str += String.fromCharCode(bytes[i]);
  return btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
function generateCodeVerifier(length = 64) {
  const charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
  const randomValues = new Uint8Array(length);
  crypto.getRandomValues(randomValues);
  let verifier = "";
  for (let i = 0; i < randomValues.length; i++) {
    verifier += charset[randomValues[i] % charset.length];
  }
  return verifier;
}
async function generateCodeChallenge(verifier) {
  const enc = new TextEncoder();
  const data = enc.encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(digest);
}

// src/tenant.js
function normalizeTenantPropagationMode(value) {
  const normalized = String(value || "all").trim().toLowerCase();
  if (normalized === "none" || normalized === "api") {
    return normalized;
  }
  return "all";
}
function getAuthTenantKey(config) {
  return normalizeTenantPropagationMode(config == null ? void 0 : config.tenantPropagationMode) === "all" ? String((config == null ? void 0 : config.tenantKey) || "").trim() : "";
}
function getApiTenantKey(config) {
  const mode = normalizeTenantPropagationMode(config == null ? void 0 : config.tenantPropagationMode);
  return mode === "all" || mode === "api" ? String((config == null ? void 0 : config.tenantKey) || "").trim() : "";
}
function resolveRawTenantKey(config, overrides = {}) {
  const explicitTenantKey = String(
    (overrides == null ? void 0 : overrides.tenantKey) || (config == null ? void 0 : config.tenantKey) || ""
  ).trim();
  if (explicitTenantKey) {
    return explicitTenantKey;
  }
  if (typeof window !== "undefined") {
    const tenantFromQuery = new URLSearchParams(window.location.search).get(
      (config == null ? void 0 : config.tenantQueryParameter) || "tenant"
    );
    if (tenantFromQuery) {
      return tenantFromQuery.trim();
    }
  }
  if (typeof sessionStorage !== "undefined") {
    const tenantFromStorage = sessionStorage.getItem(
      (config == null ? void 0 : config.tenantKeyStorageKey) || "idp_tenant_key"
    );
    if (tenantFromStorage) {
      return tenantFromStorage.trim();
    }
  }
  return "";
}
function resolveAuthTenantKey(config, overrides = {}) {
  return normalizeTenantPropagationMode(config == null ? void 0 : config.tenantPropagationMode) === "all" ? resolveRawTenantKey(config, overrides) : "";
}
function resolveApiTenantKey(config, overrides = {}) {
  const mode = normalizeTenantPropagationMode(config == null ? void 0 : config.tenantPropagationMode);
  return mode === "all" || mode === "api" ? resolveRawTenantKey(config, overrides) : "";
}

// src/oauth.js
function randomState(length = 32) {
  const bytes = new Uint8Array(length);
  crypto.getRandomValues(bytes);
  return Array.from(bytes).map((b) => b.toString(16).padStart(2, "0")).join("");
}
function buildAuthorizeUrl(config, params) {
  const url = new URL(config.authority + config.authorizePath);
  const tenantKey = getAuthTenantKey({
    ...config,
    tenantPropagationMode: normalizeTenantPropagationMode(
      config == null ? void 0 : config.tenantPropagationMode
    ),
    tenantKey: params.tenantKey || config.tenantKey
  });
  url.searchParams.set("response_type", "code");
  url.searchParams.set("client_id", config.clientId);
  url.searchParams.set("redirect_uri", config.redirectUri);
  url.searchParams.set("scope", config.scope);
  url.searchParams.set("code_challenge", params.codeChallenge);
  url.searchParams.set("code_challenge_method", "S256");
  if (params.state) url.searchParams.set("state", params.state);
  if (params.audience || config.audience)
    url.searchParams.set("audience", params.audience || config.audience);
  if (tenantKey) {
    url.searchParams.set(
      config.tenantQueryParameter || "tenant",
      tenantKey
    );
  }
  if (params.prompt) url.searchParams.set("prompt", params.prompt);
  if (params.loginHint) url.searchParams.set("login_hint", params.loginHint);
  return url.toString();
}

// src/authApi.js
async function httpPostJson(url, body, extraHeaders = {}) {
  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...extraHeaders },
    body: JSON.stringify(body)
  });
  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  if (!res.ok) {
    const msg = getErrorMessage(data, res.status);
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}
async function httpGetJson(url, extraHeaders = {}) {
  const res = await fetch(url, {
    method: "GET",
    headers: { ...extraHeaders }
  });
  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  if (!res.ok) {
    const msg = getErrorMessage(data, res.status);
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}
function getErrorMessage(data, status) {
  var _a;
  if (!data) return `HTTP ${status}`;
  const wrappedError = data.error && typeof data.error === "object" ? data.error : ((_a = data.value) == null ? void 0 : _a.error) && typeof data.value.error === "object" ? data.value.error : null;
  return data.error_description || (wrappedError == null ? void 0 : wrappedError.error) || (wrappedError == null ? void 0 : wrappedError.Error) || (wrappedError == null ? void 0 : wrappedError.message) || (wrappedError == null ? void 0 : wrappedError.Message) || (typeof data.error === "string" ? data.error : "") || data.message || `HTTP ${status}`;
}
function withTenant(url, config, target = "api") {
  const tenantKey = target === "auth" ? getAuthTenantKey(config) : getApiTenantKey(config);
  if (!tenantKey) {
    return url;
  }
  const tenantUrl = new URL(url, typeof window !== "undefined" ? window.location.origin : void 0);
  tenantUrl.searchParams.set(config.tenantQueryParameter || "tenant", tenantKey);
  return tenantUrl.toString();
}
function extractToken(tokenPayload) {
  if (!tokenPayload)
    return { accessToken: "", refreshToken: "", expiresIn: 0, idToken: "" };
  const accessToken = tokenPayload.value.accessToken || tokenPayload.value.access_token || "";
  const refreshToken = tokenPayload.value.refreshToken || tokenPayload.value.refresh_token || "";
  const expiresIn = Number(
    tokenPayload.value.expiresIn || tokenPayload.value.expires_in || 0
  ) || 0;
  const idToken = tokenPayload.idToken || tokenPayload.id_token || "";
  return { accessToken, refreshToken, expiresIn, idToken };
}
function extractPermissions(userInfo) {
  const direct = (userInfo == null ? void 0 : userInfo.permissions) || (userInfo == null ? void 0 : userInfo.Permissions) || (userInfo == null ? void 0 : userInfo.claims) || (userInfo == null ? void 0 : userInfo.Claims);
  if (Array.isArray(direct)) return direct;
  if (Array.isArray(userInfo == null ? void 0 : userInfo.permissionKeys)) return userInfo.permissionKeys;
  return [];
}
async function exchangeAuthorizationCode(config, payload) {
  const url = withTenant(config.authority + config.tokenPath, config, "auth");
  return await httpPostJson(url, payload);
}
async function refreshWithToken(config, payload) {
  const url = withTenant(config.authority + config.tokenPath, config, "auth");
  return await httpPostJson(url, payload);
}
async function revokeToken(config, { accessToken, token, reasonRevoked }) {
  if (!(config == null ? void 0 : config.authority) || !accessToken || !token) {
    return null;
  }
  const url = withTenant(
    new URL(config.revokePath || "/revoke", config.authority).toString(),
    config,
    "auth"
  );
  const res = await fetch(url, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      token,
      reasonRevoked: reasonRevoked || "logout"
    })
  });
  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  if (!res.ok) {
    const msg = getErrorMessage(data, res.status);
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}
async function loadUserPermissions(config, accessToken) {
  const url = withTenant(config.authority + config.userPermissionsPath, config, "api");
  return await httpGetJson(url, { Authorization: `Bearer ${accessToken}` });
}
function buildLogoutUrl(config) {
  if (!(config == null ? void 0 : config.authority)) return "";
  const url = new URL(
    withTenant(
      new URL(config.logoutPath || "/logout", config.authority).toString(),
      config,
      "auth"
    )
  );
  if (config.clientId) {
    url.searchParams.set("client_id", config.clientId);
  }
  const postLogoutRedirectUri = resolvePostLogoutRedirectUri(config);
  if (postLogoutRedirectUri) {
    url.searchParams.set("post_logout_redirect_uri", postLogoutRedirectUri);
  }
  return url.toString();
}
function resolvePostLogoutRedirectUri(config) {
  var _a;
  const candidate = config == null ? void 0 : config.postLogoutRedirectUri;
  if (!candidate) {
    return "";
  }
  if (typeof window !== "undefined" && ((_a = window.location) == null ? void 0 : _a.origin)) {
    return new URL(candidate, window.location.origin).toString();
  }
  if (config == null ? void 0 : config.redirectUri) {
    return new URL(candidate, config.redirectUri).toString();
  }
  return String(candidate);
}

// src/AuthProvider.jsx
var AuthContext = createContext(null);
var initialState = {
  isAuthenticated: false,
  userId: 0,
  tenantId: 0,
  tenantKey: "",
  userName: "",
  landingPage: "",
  accessToken: "",
  refreshToken: "",
  idToken: "",
  expiresAt: 0,
  error: "",
  permissions: []
};
function reducer(state, action) {
  switch (action.type) {
    case "LOGIN_SUCCESS":
      return {
        ...state,
        ...action.payload,
        isAuthenticated: true,
        error: ""
      };
    case "SET_ERROR":
      return { ...state, error: action.payload || "Unknown error" };
    case "LOGOUT":
      return { ...initialState };
    case "TOKENS_UPDATED":
      return { ...state, ...action.payload };
    default:
      return state;
  }
}
function IdpAuthProvider({ children, config }) {
  const baseConfig = useMemo(
    () => ({ ...defaultAuthConfig, ...config || {} }),
    [config]
  );
  const storage = useMemo(
    () => createStorage(baseConfig.storage),
    [baseConfig.storage]
  );
  const persistedRaw = storage.getItem(baseConfig.storageKey);
  const persisted = persistedRaw ? safeJsonParse(persistedRaw) : null;
  const mergedConfig = useMemo(() => {
    const normalizedConfig = {
      ...baseConfig,
      tenantPropagationMode: normalizeTenantPropagationMode(
        baseConfig == null ? void 0 : baseConfig.tenantPropagationMode
      )
    };
    const resolvedTenantKey = resolveApiTenantKey(normalizedConfig) || getApiTenantKey({
      ...normalizedConfig,
      tenantKey: persisted == null ? void 0 : persisted.tenantKey
    });
    return {
      ...normalizedConfig,
      tenantKey: resolvedTenantKey
    };
  }, [baseConfig, persisted == null ? void 0 : persisted.tenantKey]);
  const [state, dispatch] = useReducer(
    reducer,
    buildInitialState(persisted, mergedConfig)
  );
  useEffect(() => {
    storage.setItem(mergedConfig.storageKey, JSON.stringify(state));
  }, [state, storage, mergedConfig.storageKey]);
  const refreshTimerRef = useRef(null);
  const refreshInFlightRef = useRef(false);
  function clearRefreshTimer() {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }
  }
  function clearLocalSession() {
    storage.removeItem(mergedConfig.storageKey);
    sessionStorage.removeItem(mergedConfig.pkceVerifierKey);
    sessionStorage.removeItem(mergedConfig.oauthStateKey);
    sessionStorage.removeItem(mergedConfig.tenantKeyStorageKey);
    dispatch({ type: "LOGOUT" });
  }
  async function tryRefreshWithRetry(retries, retryDelayMs) {
    try {
      await api.refresh();
      return true;
    } catch (err) {
      if (retries > 0) {
        await new Promise((res) => setTimeout(res, retryDelayMs));
        return tryRefreshWithRetry(retries - 1, retryDelayMs);
      }
      return false;
    }
  }
  function scheduleAutoRefresh(nextExpiresAtMs) {
    clearRefreshTimer();
    if (!mergedConfig.autoRefresh || !nextExpiresAtMs) return;
    const skewMs = (mergedConfig.refreshSkewSeconds || 60) * 1e3;
    const delay = Math.max(0, nextExpiresAtMs - Date.now() - skewMs);
    refreshTimerRef.current = setTimeout(async () => {
      if (refreshInFlightRef.current) return;
      refreshInFlightRef.current = true;
      const ok = await tryRefreshWithRetry(1, 5e3);
      refreshInFlightRef.current = false;
      if (!ok) {
        api.logout();
      }
    }, delay);
  }
  useEffect(() => {
    if (!state.isAuthenticated || !state.expiresAt) return;
    scheduleAutoRefresh(state.expiresAt);
    return () => {
      clearRefreshTimer();
    };
  }, [state.isAuthenticated, state.expiresAt]);
  const api = useMemo(() => {
    return {
      ...state,
      hasPermission: (perm) => Array.isArray(state.permissions) && state.permissions.includes(perm),
      hasAnyPermission: (perms) => Array.isArray(perms) && perms.some((p) => {
        var _a;
        return (_a = state.permissions) == null ? void 0 : _a.includes(p);
      }),
      hasAllPermissions: (perms) => Array.isArray(perms) && perms.every((p) => {
        var _a;
        return (_a = state.permissions) == null ? void 0 : _a.includes(p);
      }),
      login: async (options = {}) => {
        if (!mergedConfig.authority || !mergedConfig.clientId || !mergedConfig.redirectUri) {
          throw new Error(
            "Missing authority/clientId/redirectUri in IdpAuthProvider config."
          );
        }
        const verifier = generateCodeVerifier();
        const challenge = await generateCodeChallenge(verifier);
        const stateVal = randomState();
        const authorizeTenantKey = resolveAuthTenantKey(mergedConfig, options);
        const apiTenantKey = resolveApiTenantKey(mergedConfig, options);
        sessionStorage.setItem(mergedConfig.pkceVerifierKey, verifier);
        sessionStorage.setItem(mergedConfig.oauthStateKey, stateVal);
        if (apiTenantKey) {
          sessionStorage.setItem(mergedConfig.tenantKeyStorageKey, apiTenantKey);
        } else {
          sessionStorage.removeItem(mergedConfig.tenantKeyStorageKey);
        }
        const authorizeUrl = buildAuthorizeUrl(mergedConfig, {
          codeChallenge: challenge,
          state: stateVal,
          prompt: options.prompt,
          loginHint: options.loginHint,
          audience: options.audience,
          tenantKey: authorizeTenantKey
        });
        window.location.assign(authorizeUrl);
      },
      logout: async () => {
        const logoutUrl = buildLogoutUrl(mergedConfig);
        clearRefreshTimer();
        try {
          await revokeToken(mergedConfig, {
            accessToken: state.accessToken,
            token: state.refreshToken,
            reasonRevoked: "logout"
          });
        } catch (error) {
          console.warn("Token revocation during logout failed.", error);
        }
        if (typeof window !== "undefined" && logoutUrl) {
          window.addEventListener("pagehide", clearLocalSession, { once: true });
          window.location.assign(logoutUrl);
          return;
        }
        clearLocalSession();
      },
      // exchanges code->tokens, loads permissions, stores everything
      handleCallback: async ({ code, state: returnedState }) => {
        var _a;
        const verifier = sessionStorage.getItem(mergedConfig.pkceVerifierKey);
        if (!verifier) throw new Error("Missing code verifier (PKCE).");
        const tenantKey = resolveApiTenantKey(mergedConfig);
        const expectedState = sessionStorage.getItem(
          mergedConfig.oauthStateKey
        );
        if (expectedState && returnedState && expectedState !== returnedState) {
          throw new Error("Invalid OAuth state. Possible CSRF.");
        }
        var tokenPayload = {};
        try {
          tokenPayload = await exchangeAuthorizationCode(mergedConfig, {
            grantType: "authorization_code",
            clientId: mergedConfig.clientId,
            redirectUri: mergedConfig.redirectUri,
            code,
            codeVerifier: verifier,
            scope: mergedConfig.scope
          });
        } catch (e) {
          console.error("exchangeAuthorizationCode failed:", e);
          console.error("Status:", e == null ? void 0 : e.status);
          console.error("Data:", e == null ? void 0 : e.data);
          throw e;
        }
        const { accessToken, refreshToken, expiresIn, idToken } = extractToken(tokenPayload);
        if (!accessToken)
          throw new Error("Token response did not include an access token.");
        const expiresAt = expiresIn ? Date.now() + expiresIn * 1e3 : 0;
        const userInfoResult = await loadUserPermissions(
          mergedConfig,
          accessToken
        );
        if ((userInfoResult == null ? void 0 : userInfoResult.isSuccess) === false) {
          throw new Error(
            ((_a = userInfoResult == null ? void 0 : userInfoResult.error) == null ? void 0 : _a.error) || "Unable to load user permissions."
          );
        }
        const userInfo = (userInfoResult == null ? void 0 : userInfoResult.value) || {};
        const permissions = extractPermissions(userInfo);
        const userId = userInfo.userId ?? userInfo.UserId ?? 0;
        const tenantId = userInfo.tenantId ?? userInfo.TenantId ?? 0;
        const userName = userInfo.userName ?? userInfo.UserName ?? "";
        dispatch({
          type: "LOGIN_SUCCESS",
          payload: {
            userId,
            tenantId,
            tenantKey,
            userName,
            permissions,
            accessToken,
            refreshToken: refreshToken || "",
            idToken: idToken || "",
            expiresAt,
            landingPage: mergedConfig.postLoginRedirectUri || "/"
          }
        });
        sessionStorage.removeItem(mergedConfig.pkceVerifierKey);
        sessionStorage.removeItem(mergedConfig.oauthStateKey);
        return {
          userId,
          tenantId,
          tenantKey,
          userName,
          permissions,
          accessToken,
          refreshToken: refreshToken || "",
          idToken: idToken || "",
          expiresAt
        };
      },
      refresh: async () => {
        if (!state.refreshToken) throw new Error("No refresh token available.");
        const tokenPayload = await refreshWithToken(mergedConfig, {
          grantType: "refresh_token",
          clientId: mergedConfig.clientId,
          refreshToken: state.refreshToken,
          scope: mergedConfig.scope
        });
        const { accessToken, refreshToken, expiresIn, idToken } = extractToken(tokenPayload);
        if (!accessToken)
          throw new Error("Refresh response did not include an access token.");
        const expiresAt = expiresIn ? Date.now() + expiresIn * 1e3 : 0;
        dispatch({
          type: "TOKENS_UPDATED",
          payload: {
            accessToken,
            // if rotation: use new refresh token if provided
            refreshToken: refreshToken || state.refreshToken,
            idToken: idToken || state.idToken,
            expiresAt
          }
        });
        return {
          accessToken,
          refreshToken: refreshToken || state.refreshToken,
          idToken,
          expiresAt
        };
      },
      setError: (message) => dispatch({ type: "SET_ERROR", payload: message })
    };
  }, [state, mergedConfig, storage]);
  return /* @__PURE__ */ React.createElement(AuthContext.Provider, { value: api }, children);
}
function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside IdpAuthProvider");
  return ctx;
}
function safeJsonParse(raw) {
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}
function buildInitialState(persistedState, config) {
  if (!persistedState) {
    return {
      ...initialState,
      tenantKey: config.tenantKey
    };
  }
  return {
    ...initialState,
    ...persistedState,
    tenantKey: config.tenantKey
  };
}

// src/AuthCallback.jsx
import React2, { useEffect as useEffect2, useRef as useRef2, useState } from "react";
import { useNavigate } from "react-router-dom";
function renderLogoContent(logo, logoAlt, fallback) {
  if (!logo) {
    return /* @__PURE__ */ React2.createElement("div", { className: "logo mb-3" }, fallback);
  }
  if (typeof logo === "string") {
    return /* @__PURE__ */ React2.createElement("div", { className: "mb-3" }, /* @__PURE__ */ React2.createElement("img", { src: logo, alt: logoAlt, width: "250" }));
  }
  return /* @__PURE__ */ React2.createElement("div", { className: "mb-3" }, logo);
}
function AuthCallback({
  redirectTo,
  logo = null,
  logoAlt = "Application logo",
  fallbackBadge = "ID"
}) {
  const navigate = useNavigate();
  const auth = useAuth();
  const [error, setError] = useState(null);
  const ranRef = useRef2(false);
  useEffect2(() => {
    const run = async () => {
      if (ranRef.current) return;
      ranRef.current = true;
      const qs = new URLSearchParams(window.location.search);
      const code = qs.get("code");
      const state = qs.get("state");
      const err = qs.get("error");
      const errDesc = qs.get("error_description");
      if (err) {
        const msg = errDesc || err || "Authentication error.";
        setError(msg);
        auth.setError(msg);
        return;
      }
      if (!code || !state) {
        const msg = "Missing authorization code or state.";
        setError(msg);
        auth.setError(msg);
        return;
      }
      try {
        await auth.handleCallback({ code, state });
        const target = redirectTo || auth.landingPage || "/";
        navigate(target, { replace: true });
      } catch (e) {
        const msg = (e == null ? void 0 : e.message) || "Login callback failed.";
        setError(msg);
        auth.setError(msg);
      }
    };
    run();
  }, [auth, navigate, redirectTo]);
  return /* @__PURE__ */ React2.createElement("div", { className: "login-page" }, /* @__PURE__ */ React2.createElement("section", { className: "login-section-container" }, /* @__PURE__ */ React2.createElement("div", { className: "login-section redirect-card" }, /* @__PURE__ */ React2.createElement("div", { className: "col-12 p-4 text-center" }, renderLogoContent(logo, logoAlt, fallbackBadge), /* @__PURE__ */ React2.createElement("h1", null, "Completing sign-in"), /* @__PURE__ */ React2.createElement("p", { className: "text-muted" }, error ? error : "Please wait while we finish authentication.")))));
}

// src/LoginPage.jsx
import React3, { useEffect as useEffect3 } from "react";
function renderLogoContent2(logo, logoAlt, fallback) {
  if (!logo) {
    return /* @__PURE__ */ React3.createElement("div", { className: "logo mb-3" }, fallback);
  }
  if (typeof logo === "string") {
    return /* @__PURE__ */ React3.createElement("div", { className: "mb-3" }, /* @__PURE__ */ React3.createElement("img", { src: logo, alt: logoAlt, width: "250" }));
  }
  return /* @__PURE__ */ React3.createElement("div", { className: "mb-3" }, logo);
}
function LoginPage({
  logo = null,
  logoAlt = "Application logo",
  title = "Redirecting to sign-in...",
  subtitle = "Please wait while we securely connect to Identity.",
  signedOutBadge = "Signed out",
  signedOutTitle = "You have been signed out",
  signedOutSubtitle = "Start a new session when you are ready.",
  signInAgainLabel = "Sign in again",
  fallbackBadge = "ID",
  loginOptions
}) {
  const auth = useAuth();
  const isLoggedOut = new URLSearchParams(window.location.search).get("logged_out") === "1";
  useEffect3(() => {
    if (isLoggedOut) {
      return;
    }
    auth.login(loginOptions);
  }, [auth, isLoggedOut, loginOptions]);
  if (isLoggedOut) {
    return /* @__PURE__ */ React3.createElement("div", { className: "login-page" }, /* @__PURE__ */ React3.createElement("section", { className: "login-section-container" }, /* @__PURE__ */ React3.createElement("div", { className: "login-section redirect-card" }, /* @__PURE__ */ React3.createElement("div", { className: "p-4 text-center" }, renderLogoContent2(logo, logoAlt, fallbackBadge), /* @__PURE__ */ React3.createElement("h1", null, signedOutTitle), /* @__PURE__ */ React3.createElement("p", { className: "text-muted" }, signedOutSubtitle), /* @__PURE__ */ React3.createElement(
      "button",
      {
        className: "btn btn-primary mt-3",
        onClick: () => auth.login(loginOptions)
      },
      signInAgainLabel
    )))));
  }
  return /* @__PURE__ */ React3.createElement("div", { className: "login-page" }, /* @__PURE__ */ React3.createElement("section", { className: "login-section-container" }, /* @__PURE__ */ React3.createElement("div", { className: "login-section redirect-card" }, /* @__PURE__ */ React3.createElement("div", { className: "p-4 text-center" }, renderLogoContent2(logo, logoAlt, fallbackBadge), /* @__PURE__ */ React3.createElement("h1", null, title), /* @__PURE__ */ React3.createElement("p", { className: "text-muted" }, subtitle), /* @__PURE__ */ React3.createElement("div", { className: "spinner mt-3" })))));
}
export {
  AuthCallback,
  IdpAuthProvider,
  LoginPage,
  defaultAuthConfig,
  useAuth
};
//# sourceMappingURL=index.mjs.map