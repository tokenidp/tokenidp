import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
} from "react";
import { defaultAuthConfig } from "./config";
import { createStorage } from "./storage";
import { generateCodeVerifier, generateCodeChallenge } from "./pkce";
import { buildAuthorizeUrl, randomState } from "./oauth";
import {
  getApiTenantKey,
  normalizeTenantPropagationMode,
  resolveApiTenantKey,
  resolveAuthTenantKey,
} from "./tenant";
import {
  exchangeAuthorizationCode,
  refreshWithToken,
  revokeToken,
  loadUserPermissions,
  extractToken,
  extractPermissions,
  buildLogoutUrl,
} from "./authApi";

const AuthContext = createContext(null);

const initialState = {
  isAuthenticated: false,
  userId: 0,
  tenantId: 0,
  tenantKey: "",
  isSystemTenant: false,
  userName: "",
  landingPage: "",
  accessToken: "",
  refreshToken: "",
  idToken: "",
  expiresAt: 0,
  error: "",
  permissions: [],
};

function reducer(state, action) {
  switch (action.type) {
    case "LOGIN_SUCCESS":
      return {
        ...state,
        ...action.payload,
        isAuthenticated: true,
        error: "",
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

export function IdpAuthProvider({ children, config }) {
  const baseConfig = useMemo(
    () => ({ ...defaultAuthConfig, ...(config || {}) }),
    [config],
  );

  const storage = useMemo(
    () => createStorage(baseConfig.storage),
    [baseConfig.storage],
  );

  const persistedRaw = storage.getItem(baseConfig.storageKey);
  const persisted = persistedRaw ? safeJsonParse(persistedRaw) : null;

  const mergedConfig = useMemo(() => {
    const normalizedConfig = {
      ...baseConfig,
      tenantPropagationMode: normalizeTenantPropagationMode(
        baseConfig?.tenantPropagationMode,
      ),
    };
    const resolvedTenantKey =
      resolveApiTenantKey(normalizedConfig) ||
      getApiTenantKey({
        ...normalizedConfig,
        tenantKey: persisted?.tenantKey,
      });

    return {
      ...normalizedConfig,
      tenantKey: resolvedTenantKey,
    };
  }, [baseConfig, persisted?.tenantKey]);

  const [state, dispatch] = useReducer(
    reducer,
    buildInitialState(persisted, mergedConfig),
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

    const skewMs = (mergedConfig.refreshSkewSeconds || 60) * 1000;
    const delay = Math.max(0, nextExpiresAtMs - Date.now() - skewMs);

    refreshTimerRef.current = setTimeout(async () => {
      if (refreshInFlightRef.current) return;
      refreshInFlightRef.current = true;

      const ok = await tryRefreshWithRetry(1, 5000); // 👈 1 retry

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

      hasPermission: (perm) =>
        Array.isArray(state.permissions) && state.permissions.includes(perm),
      hasAnyPermission: (perms) =>
        Array.isArray(perms) &&
        perms.some((p) => state.permissions?.includes(p)),
      hasAllPermissions: (perms) =>
        Array.isArray(perms) &&
        perms.every((p) => state.permissions?.includes(p)),

      login: async (options = {}) => {
        if (
          !mergedConfig.authority ||
          !mergedConfig.clientId ||
          !mergedConfig.redirectUri
        ) {
          throw new Error(
            "Missing authority/clientId/redirectUri in IdpAuthProvider config.",
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
          tenantKey: authorizeTenantKey,
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
            reasonRevoked: "logout",
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
        const verifier = sessionStorage.getItem(mergedConfig.pkceVerifierKey);
        if (!verifier) throw new Error("Missing code verifier (PKCE).");
        const tenantKey = resolveApiTenantKey(mergedConfig);

        const expectedState = sessionStorage.getItem(
          mergedConfig.oauthStateKey,
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
            scope: mergedConfig.scope,
          });
        } catch (e) {
          console.error("exchangeAuthorizationCode failed:", e);
          console.error("Status:", e?.status);
          console.error("Data:", e?.data);
          throw e;
        }

        const { accessToken, refreshToken, expiresIn, idToken } =
          extractToken(tokenPayload);

        if (!accessToken)
          throw new Error("Token response did not include an access token.");

        const expiresAt = expiresIn ? Date.now() + expiresIn * 1000 : 0;

        // permissions/user info
        const userInfoResult = await loadUserPermissions(
          mergedConfig,
          accessToken,
        );

        // Your API pattern: { isSuccess, value, error }
        if (userInfoResult?.isSuccess === false) {
          throw new Error(
            userInfoResult?.error?.error || "Unable to load user permissions.",
          );
        }

        const userInfo = userInfoResult?.value || {};
        const permissions = extractPermissions(userInfo);

        const userId = userInfo.userId ?? userInfo.UserId ?? 0;
        const tenantId = userInfo.tenantId ?? userInfo.TenantId ?? 0;
        const responseTenantKey = userInfo.tenantKey ?? userInfo.TenantKey ?? tenantKey;
        const isSystemTenant =
          userInfo.isSystemTenant ?? userInfo.IsSystemTenant ?? false;
        const userName = userInfo.userName ?? userInfo.UserName ?? "";

        dispatch({
          type: "LOGIN_SUCCESS",
          payload: {
            userId,
            tenantId,
            tenantKey: responseTenantKey,
            isSystemTenant,
            userName,
            permissions,
            accessToken,
            refreshToken: refreshToken || "",
            idToken: idToken || "",
            expiresAt,
            landingPage: mergedConfig.postLoginRedirectUri || "/",
          },
        });

        // cleanup PKCE state
        sessionStorage.removeItem(mergedConfig.pkceVerifierKey);
        sessionStorage.removeItem(mergedConfig.oauthStateKey);

        return {
          userId,
          tenantId,
          tenantKey: responseTenantKey,
          isSystemTenant,
          userName,
          permissions,
          accessToken,
          refreshToken: refreshToken || "",
          idToken: idToken || "",
          expiresAt,
        };
      },

      refresh: async () => {
        if (!state.refreshToken) throw new Error("No refresh token available.");

        const tokenPayload = await refreshWithToken(mergedConfig, {
          grantType: "refresh_token",
          clientId: mergedConfig.clientId,
          refreshToken: state.refreshToken,
          scope: mergedConfig.scope,
        });

        const { accessToken, refreshToken, expiresIn, idToken } =
          extractToken(tokenPayload);
        if (!accessToken)
          throw new Error("Refresh response did not include an access token.");

        const expiresAt = expiresIn ? Date.now() + expiresIn * 1000 : 0;

        dispatch({
          type: "TOKENS_UPDATED",
          payload: {
            accessToken,
            // if rotation: use new refresh token if provided
            refreshToken: refreshToken || state.refreshToken,
            idToken: idToken || state.idToken,
            expiresAt,
          },
        });

        return {
          accessToken,
          refreshToken: refreshToken || state.refreshToken,
          idToken,
          expiresAt,
        };
      },

      setError: (message) => dispatch({ type: "SET_ERROR", payload: message }),
    };
  }, [state, mergedConfig, storage]);

  return <AuthContext.Provider value={api}>{children}</AuthContext.Provider>;
}

export function useAuth() {
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
      tenantKey: config.tenantKey,
    };
  }

  return {
    ...initialState,
    ...persistedState,
    tenantKey: config.tenantKey,
  };
}
