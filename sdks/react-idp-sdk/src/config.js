export const defaultAuthConfig = {
  authority: "", // e.g. https://idp.tokentresor.com
  clientId: "",
  tenantKey: "",
  tenantQueryParameter: "tenant",
  tenantHeaderName: "X-Tenant-Key",
  tenantKeyStorageKey: "idp_tenant_key",
  redirectUri: "", // e.g. https://app.com/auth/callback
  postLoginRedirectUri: "/", // where to go after success
  postLogoutRedirectUri: "/login", // where to go after logout
  scope: "openid profile offline_access",
  audience: "", // optional
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
  refreshSkewSeconds: 180,
};
