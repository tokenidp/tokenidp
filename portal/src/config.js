const runtimeConfigKey = "__TOKENIDP_ADMIN_PORTAL_CONFIG__";

const envConfig = {
  deploymentEnvironment:
    process.env.REACT_APP_DEPLOYMENT_ENV || process.env.NODE_ENV || "development",
  baseUrl: process.env.REACT_APP_BASE_URL,
  authority: process.env.REACT_APP_AUTH_BASE_URL,
  tenantPropagationMode: process.env.REACT_APP_TENANT_PROPAGATION_MODE || "all",
  tenantQueryParameter: process.env.REACT_APP_TENANT_QUERY_PARAMETER || "tenant",
  userPermissionsPath:
    process.env.REACT_APP_USER_PERMISSIONS_PATH || "admin/user/permissions",
  clientId: process.env.REACT_APP_OAUTH_CLIENT_ID,
  redirectUri: process.env.REACT_APP_OAUTH_REDIRECT_URI,
  postLoginRedirectUri: "/dashboard",
  postLogoutRedirectUri:
    process.env.REACT_APP_OAUTH_POST_LOGOUT_REDIRECT_URI || "/login",
  scope: process.env.REACT_APP_OAUTH_SCOPE,
};

const normalizeConfig = (config = {}) => ({
  ...envConfig,
  ...config,
  baseUrl: config.baseUrl || config.baseURL || config.apiBaseUrl || envConfig.baseUrl,
  authority: config.authority || config.authBaseUrl || envConfig.authority,
  clientId: config.clientId || config.oauthClientId || envConfig.clientId,
  redirectUri: config.redirectUri || config.oauthRedirectUri || envConfig.redirectUri,
  postLoginRedirectUri:
    config.postLoginRedirectUri || envConfig.postLoginRedirectUri,
  postLogoutRedirectUri:
    config.postLogoutRedirectUri ||
    config.oauthPostLogoutRedirectUri ||
    envConfig.postLogoutRedirectUri,
  scope: config.scope || config.oauthScope || envConfig.scope,
  tenantPropagationMode:
    config.tenantPropagationMode || envConfig.tenantPropagationMode,
  tenantQueryParameter:
    config.tenantQueryParameter || envConfig.tenantQueryParameter,
  userPermissionsPath:
    config.userPermissionsPath || envConfig.userPermissionsPath,
});

export async function loadPortalConfig() {
  try {
    const response = await fetch(`${process.env.PUBLIC_URL || ""}/config.json`, {
      cache: "no-store",
    });

    if (!response.ok) {
      throw new Error(`Config request failed (${response.status}).`);
    }

    const config = await response.json();
    window[runtimeConfigKey] = normalizeConfig(config);
  } catch {
    window[runtimeConfigKey] = normalizeConfig();
  }

  return window[runtimeConfigKey];
}

export function getPortalConfig() {
  return window[runtimeConfigKey] || normalizeConfig();
}
