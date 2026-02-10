export const defaultAuthConfig = {
  authority: "", // e.g. https://idp.tokentresor.com
  clientId: "",
  redirectUri: "", // e.g. https://app.com/auth/callback
  postLoginRedirectUri: "/", // where to go after success
  scope: "openid profile offline_access",
  audience: "", // optional
  // endpoints (default paths)
  authorizePath: "/authorize",
  tokenPath: "/token",
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
  refreshSkewSeconds: 60,
};
